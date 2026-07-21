using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PhoenixWatcho.Data;
using PhoenixWatcho.Models;
using PhoenixWatcho.Services;

namespace PhoenixWatcho.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController(AppDbContext db, IMemoryCache cache) : ControllerBase
{
    // ── Subscribers ─────────────────────────────────────────────────────────

    [HttpGet("subscribers")]
    public async Task<IActionResult> GetSubscribers()
    {
        var subs = await db.Subscribers
            .AsNoTracking()
            .Select(s => new
            {
                s.Id, s.VcNumber, s.FullName, s.Email, s.Phone, s.City, s.State, s.RegisteredAt,
                CurrentPlan = db.Subscriptions
                    .Where(sub => sub.SubscriberId == s.Id)
                    .OrderByDescending(sub => sub.ExpiryDate)
                    .Select(sub => sub.Plan!.Name)
                    .FirstOrDefault(),
                ExpiryDate = db.Subscriptions
                    .Where(sub => sub.SubscriberId == s.Id)
                    .OrderByDescending(sub => sub.ExpiryDate)
                    .Select(sub => (DateTime?)sub.ExpiryDate)
                    .FirstOrDefault(),
                PendingDocs = db.KycDocuments.Count(d => d.SubscriberId == s.Id && d.Status == "Pending"),
            })
            .OrderByDescending(s => s.RegisteredAt)
            .ToListAsync();

        return Ok(subs);
    }

    // ── Plans ───────────────────────────────────────────────────────────────

    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans() =>
        Ok(await db.Plans.AsNoTracking().OrderBy(p => p.MonthlyPrice).ToListAsync());

    // ── KYC Documents ───────────────────────────────────────────────────────

    [HttpGet("documents")]
    public async Task<IActionResult> GetDocuments([FromQuery] string? status = null)
    {
        var query = db.KycDocuments
            .AsNoTracking()
            .Include(d => d.Subscriber)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(d => d.Status == status);

        var docs = await query
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new
            {
                d.Id, d.DocumentType, d.FileName, d.FilePath, d.Status, d.UploadedAt,
                SubscriberName = d.Subscriber!.FullName,
                VcNumber = d.Subscriber.VcNumber
            })
            .ToListAsync();

        return Ok(docs);
    }

    [HttpPut("document/{id}/status")]
    public async Task<IActionResult> UpdateDocumentStatus(int id, [FromBody] UpdateStatusRequest req)
    {
        var doc = await db.KycDocuments.FindAsync(id);
        if (doc == null) return NotFound("Document not found.");

        doc.Status = req.Status; // Pending, Approved, Rejected
        await db.SaveChangesAsync();
        return Ok(new { message = $"Document {req.Status}.", doc.Status });
    }

    // ── Chat Logs ───────────────────────────────────────────────────────────

    [HttpGet("chatlogs")]
    public async Task<IActionResult> GetChatLogs([FromQuery] int take = 50)
    {
        var logs = await db.ChatLogs
            .AsNoTracking()
            .OrderByDescending(l => l.CreatedAt)
            .Take(take)
            .ToListAsync();

        return Ok(logs);
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics()
    {
        var topicCounts = await db.ChatLogs
            .GroupBy(l => l.Topic)
            .Select(g => new { Topic = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        var totalChats       = await db.ChatLogs.CountAsync();
        var totalSubscribers = await db.Subscribers.CountAsync();
        var expiredCount     = await db.Subscriptions
            .GroupBy(s => s.SubscriberId)
            .Select(g => g.OrderByDescending(x => x.ExpiryDate).First())
            .CountAsync(s => s.ExpiryDate < DateTime.UtcNow);

        return Ok(new { totalChats, totalSubscribers, expiredCount, topicCounts });
    }

    // ── Knowledge Base CRUD ─────────────────────────────────────────────────

    [HttpGet("kb")]
    public async Task<IActionResult> GetKb() =>
        Ok(await db.KnowledgeBase.AsNoTracking().ToListAsync());

    [HttpPost("kb")]
    public async Task<IActionResult> AddKb([FromBody] KnowledgeBase entry)
    {
        entry.Id = 0;
        db.KnowledgeBase.Add(entry);
        await db.SaveChangesAsync();
        cache.Remove("kb_vectors");
        return Ok(entry);
    }

    [HttpPut("kb/{id}")]
    public async Task<IActionResult> UpdateKb(int id, [FromBody] KnowledgeBase entry)
    {
        var existing = await db.KnowledgeBase.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Topic    = entry.Topic;
        existing.Question = entry.Question;
        existing.Answer   = entry.Answer;
        existing.Keywords = entry.Keywords;

        await db.SaveChangesAsync();
        cache.Remove("kb_vectors"); // invalidate so chatbot picks up changes
        return Ok(existing);
    }

    [HttpDelete("kb/{id}")]
    public async Task<IActionResult> DeleteKb(int id)
    {
        var entry = await db.KnowledgeBase.FindAsync(id);
        if (entry == null) return NotFound();

        db.KnowledgeBase.Remove(entry);
        await db.SaveChangesAsync();
        cache.Remove("kb_vectors");
        cache.Remove("kb_suggestions");
        return Ok(new { message = "Deleted." });
    }

    // ── RAG ingestion ─────────────────────────────────────────────────────────

    /// <summary>
    /// Embed every knowledge-base entry with OpenAI and upsert the vectors into
    /// Supabase pgvector. Run this once after seeding, and whenever the KB changes.
    /// </summary>
    [HttpPost("reindex")]
    public async Task<IActionResult> Reindex(
        [FromServices] EmbeddingService embedder,
        [FromServices] SupabaseVectorService supabase)
    {
        if (!embedder.IsConfigured)
            return BadRequest("OpenAI key not configured — add OpenAI:Key in appsettings.Development.json.");
        if (!supabase.IsConfigured)
            return BadRequest("Supabase not configured — add Supabase:Url and Supabase:ServiceKey in appsettings.Development.json.");

        var entries = await db.KnowledgeBase.AsNoTracking().ToListAsync();
        var docs = new List<KbVectorDoc>();
        foreach (var e in entries)
        {
            // Embed question + answer + keywords for the richest match signal.
            var embedding = await embedder.EmbedAsync($"{e.Question}\n{e.Answer}\n{e.Keywords}");
            if (embedding is not null)
                docs.Add(new KbVectorDoc(e.Id, e.Topic, e.Answer, embedding));
        }

        await supabase.UpsertAsync(docs);
        return Ok(new { indexed = docs.Count, of = entries.Count });
    }
}

public record UpdateStatusRequest(string Status);
