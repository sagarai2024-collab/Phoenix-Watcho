using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using PhoenixWatcho.Data;
using PhoenixWatcho.Models;
using PhoenixWatcho.Services;

namespace PhoenixWatcho.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController(IConfiguration config, AppDbContext db, Kernel kernel,
                            IMemoryCache cache, VectorSearchService vectorSearch) : ControllerBase
{
    private const string SystemPrompt = """
        You are the DishTV / Watcho customer support assistant.
        Answer questions only using the context provided. Be friendly and concise.
        Amounts are in Indian Rupees (₹).
        """;

    [HttpGet("diagnose")]
    public IActionResult Diagnose()
    {
        var key = config["OpenAI:Key"] ?? "(missing)";
        var skEnabled = kernel.Services.GetService<IChatCompletionService>() != null;
        return Ok(new
        {
            keyPreview = key.Length > 6 ? key[..6] + "..." : key,
            keyLength  = key.Length,
            model      = config["OpenAI:Model"] ?? "(missing)",
            hasRealKey = key.StartsWith("sk-") && key.Length > 20,
            skEnabled
        });
    }

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest("Message is required.");

        var reply = await GetReplyAsync(request);

        db.ChatLogs.Add(new ChatLog
        {
            UserMessage = request.Message,
            BotReply    = reply,
            Topic       = DetectTopic(request.Message.ToLower()),
            CreatedAt   = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        return Ok(new { reply });
    }

    private async Task<string> GetReplyAsync(ChatRequest request)
    {
        var msg = request.Message.ToLower().Trim();

        // ── 1. VC NUMBER LOOKUP — 11-digit viewing card number ────────────────
        var vcMatch = Regex.Match(request.Message, @"\b0\d{10}\b");
        if (vcMatch.Success)
        {
            var vc = vcMatch.Value;
            var subscriber = await db.Subscribers.AsNoTracking()
                .FirstOrDefaultAsync(s => s.VcNumber == vc);

            if (subscriber == null)
                return $"❌ VC number '{vc}' was not found in our records.\n" +
                       "Please check the 11-digit number on the smart card inside your set-top box.";

            var sub = await db.Subscriptions.AsNoTracking().Include(s => s.Plan)
                .Where(s => s.SubscriberId == subscriber.Id)
                .OrderByDescending(s => s.ExpiryDate)
                .FirstOrDefaultAsync();

            if (sub?.Plan == null)
                return $"✅ VC {vc} — {subscriber.FullName}\nNo active plan found. Please recharge to start a pack.";

            var statusLine = sub.Status switch
            {
                "Expired"  => $"🔴 EXPIRED {Math.Abs(sub.DaysRemaining)} day(s) ago — please recharge to resume service.",
                "Expiring" => $"🟠 Expiring in {sub.DaysRemaining} day(s) — recharge soon to avoid interruption.",
                _          => $"🟢 Active — {sub.DaysRemaining} day(s) remaining."
            };
            return $"✅ VC {vc} — {subscriber.FullName}\n" +
                   $"• Plan: {sub.Plan.Name} (₹{sub.Plan.MonthlyPrice:0})\n" +
                   $"• Valid till: {sub.ExpiryDate:dd MMM yyyy}\n" +
                   $"• Status: {statusLine}";
        }

        // ── 2. GREETINGS ──────────────────────────────────────────────────────
        var greetingWords = new[] { "hi ", "hi!", "hi,", "hey", "hello", "good morning", "good evening", "good afternoon" };
        bool isGreeting = greetingWords.Any(g => msg.StartsWith(g)) || msg == "hi";
        if (isGreeting)
            return "Hello! Welcome to DishTV / Watcho support. 📺 How can I help you today?\n\n" +
                   "I can help with:\n• Plans & prices\n• Recharge & expiry\n• NCF / HD charges\n• KYC & new connection";

        // ── 3. THANKS & BYE ───────────────────────────────────────────────────
        if (msg.Contains("thank"))
            return "You're welcome! Enjoy your entertainment with DishTV. 😊";
        if (msg.Contains("bye") || msg.Contains("goodbye"))
            return "Goodbye! Recharge on time and keep watching. 👋";

        // ── 4. SK RAG — RETRIEVE → AUGMENT → GENERATE ─────────────────────────
        var context = await RetrieveAsync(msg);
        if (context != null)
        {
            var chatService = kernel.Services.GetService<IChatCompletionService>();
            if (chatService != null)
            {
                var prompt = $"{SystemPrompt}\n\nContext:\n{context}\n\nSubscriber question: {request.Message}\n\nAnswer:";
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    var result = await kernel.InvokePromptAsync(prompt, cancellationToken: cts.Token);
                    return result.ToString();
                }
                catch (Exception ex) { return $"AI Error: {ex.Message}"; }
            }
            return context; // no AI key → return grounded KB answer directly
        }

        // ── 5. DEFAULT ────────────────────────────────────────────────────────
        return "I can help you with:\n• Plans & prices\n• Recharge & plan expiry\n• NCF & HD charges\n• Discounts & offers\n• KYC documents\n\n" +
               "Tip: type your 11-digit VC number and I'll show your current plan & expiry.";
    }

    // HYBRID RAG RETRIEVAL — vector search (cosine similarity) first, keyword fallback.
    private async Task<string?> RetrieveAsync(string query)
    {
        var kbVectors = await cache.GetOrCreateAsync("kb_vectors", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            var all = await db.KnowledgeBase.AsNoTracking().ToListAsync();
            return all.Select(e =>
                (Entry: e, Vector: vectorSearch.Vectorize($"{e.Keywords} {e.Question} {e.Answer}")))
                .ToList();
        });

        var match = vectorSearch.Search(query, kbVectors!, threshold: 0.15);
        if (match != null) return match.Value.Entry.Answer;

        var queryWords = query
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim('?', '.', '!', ','))
            .Where(w => w.Length > 2)
            .ToList();

        if (queryWords.Count == 0) return null;

        var best = kbVectors!
            .Select(kv =>
            {
                var text  = $"{kv.Entry.Keywords} {kv.Entry.Question} {kv.Entry.Answer}".ToLower();
                var score = (double)queryWords.Count(w => text.Contains(w)) / queryWords.Count;
                return new { kv.Entry, Score = score };
            })
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        return best?.Score >= 0.3 ? best.Entry.Answer : null;
    }

    private static string DetectTopic(string msg)
    {
        if (msg.Contains("recharge") || msg.Contains("pay") || msg.Contains("renew"))   return "recharge";
        if (msg.Contains("plan") || msg.Contains("pack") || msg.Contains("price"))       return "plan";
        if (msg.Contains("expire") || msg.Contains("expiry") || msg.Contains("validity")) return "expiry";
        if (msg.Contains("ncf"))                                                          return "ncf";
        if (msg.Contains("hd"))                                                           return "hd";
        if (msg.Contains("discount") || msg.Contains("offer"))                            return "discount";
        if (msg.Contains("kyc") || msg.Contains("document") || msg.Contains("aadhaar"))   return "kyc";
        if (msg.Contains("support") || msg.Contains("help") || msg.Contains("contact"))   return "support";
        return "general";
    }
}

public record ChatHistory(string Role, string Content);
public record ChatRequest(string Message, List<ChatHistory> History);
