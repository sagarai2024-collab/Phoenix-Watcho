using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StudentLearningPortal.Data;
using StudentLearningPortal.Models;

namespace StudentLearningPortal.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController(AppDbContext db, IMemoryCache cache) : ControllerBase
{
    // ── Students ──────────────────────────────────────────────────────────────

    [HttpGet("students")]
    public async Task<IActionResult> GetStudents()
    {
        var students = await db.Students
            .AsNoTracking()
            .Select(s => new
            {
                s.Id, s.FullName, s.Email, s.Course, s.Phone,
                s.Gender, s.RegisteredAt,
                DocumentCount  = db.StudentDocuments.Count(d => d.StudentId == s.Id),
                PendingDocs    = db.StudentDocuments.Count(d => d.StudentId == s.Id && d.Status == "Pending"),
                ApprovedDocs   = db.StudentDocuments.Count(d => d.StudentId == s.Id && d.Status == "Approved"),
            })
            .OrderByDescending(s => s.RegisteredAt)
            .ToListAsync();

        return Ok(students);
    }

    // ── Documents ─────────────────────────────────────────────────────────────

    [HttpGet("documents")]
    public async Task<IActionResult> GetDocuments([FromQuery] string? status = null)
    {
        var query = db.StudentDocuments
            .AsNoTracking()
            .Include(d => d.Student)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(d => d.Status == status);

        var docs = await query
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new
            {
                d.Id, d.DocumentType, d.FileName, d.FilePath,
                d.Status, d.UploadedAt,
                StudentName = d.Student!.FullName,
                StudentEmail = d.Student.Email
            })
            .ToListAsync();

        return Ok(docs);
    }

    [HttpPut("document/{id}/status")]
    public async Task<IActionResult> UpdateDocumentStatus(int id, [FromBody] UpdateStatusRequest req)
    {
        var doc = await db.StudentDocuments.FindAsync(id);
        if (doc == null) return NotFound("Document not found.");

        doc.Status = req.Status; // Pending, Approved, Rejected
        await db.SaveChangesAsync();
        return Ok(new { message = $"Document {req.Status}.", doc.Status });
    }

    // ── Chat Logs ─────────────────────────────────────────────────────────────

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

        var totalChats    = await db.ChatLogs.CountAsync();
        var totalStudents = await db.Students.CountAsync();
        var pendingDocs   = await db.StudentDocuments.CountAsync(d => d.Status == "Pending");

        return Ok(new { totalChats, totalStudents, pendingDocs, topicCounts });
    }

    // ── Knowledge Base CRUD ───────────────────────────────────────────────────

    [HttpGet("kb")]
    public async Task<IActionResult> GetKb() =>
        Ok(await db.KnowledgeBase.AsNoTracking().ToListAsync());

    [HttpPost("kb")]
    public async Task<IActionResult> AddKb([FromBody] KnowledgeBase entry)
    {
        entry.Id = 0;
        db.KnowledgeBase.Add(entry);
        await db.SaveChangesAsync();
        cache.Remove("kb_entries"); // invalidate cache
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
        cache.Remove("kb_entries"); // invalidate cache so chatbot picks up changes
        return Ok(existing);
    }

    [HttpDelete("kb/{id}")]
    public async Task<IActionResult> DeleteKb(int id)
    {
        var entry = await db.KnowledgeBase.FindAsync(id);
        if (entry == null) return NotFound();

        db.KnowledgeBase.Remove(entry);
        await db.SaveChangesAsync();
        cache.Remove("kb_entries");
        return Ok(new { message = "Deleted." });
    }
}

public record UpdateStatusRequest(string Status);
