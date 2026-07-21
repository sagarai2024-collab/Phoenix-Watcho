using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using PhoenixWatcho.Data;
using PhoenixWatcho.Models;

namespace PhoenixWatcho.Services;

/// <summary>
/// The DishTV RAG pipeline: intent routing + caching (cost control) → retrieve
/// (embeddings + Supabase pgvector when configured, else local cosine search) →
/// augment with a hallucination-prevention prompt → generate via Semantic Kernel
/// → fallback when nothing relevant is found.
/// </summary>
public class RagService(
    AppDbContext db,
    Kernel kernel,
    IMemoryCache cache,
    VectorSearchService local,
    EmbeddingService embedder,
    SupabaseVectorService supabase)
{
    // Answer ONLY from context — the core of hallucination prevention.
    private const string SystemPrompt = """
        You are the DishTV / Watcho customer support assistant.
        Use ONLY the CONTEXT below to answer the question.
        If the answer is not contained in the CONTEXT, reply exactly:
        "I'm not sure about that — please contact DishTV support on 1800-200-8080."
        Be concise and friendly. Use ₹ for amounts. Never invent plans, prices, channels or dates.
        """;

    private const double LiveThreshold  = 0.75; // cosine similarity on real embeddings
    private const double LocalThreshold = 0.15; // cosine similarity on bag-of-words vectors

    public async Task<RagResult> AnswerAsync(string message, List<ChatHistory>? history)
    {
        var msg   = (message ?? "").Trim();
        var lower = msg.ToLower();

        // 1. VC-number lookup (account-grounded answer) — never hits the LLM.
        var vc = Regex.Match(msg, @"\b0\d{10}\b");
        if (vc.Success) return await VcLookupAsync(vc.Value);

        // 2. Intent fast-paths (greetings/thanks/bye) — never hit the LLM.
        var quick = QuickIntent(lower);
        if (quick != null) return new RagResult(quick, true, new(), Suggestions());

        // 3. Response cache — repeated/identical questions skip retrieval + the LLM.
        var cacheKey = "chat:" + Normalize(lower);
        if (cache.TryGetValue(cacheKey, out RagResult? cached) && cached is not null)
            return cached;

        // 4. Retrieve relevant context.
        var (context, sources, found) = await RetrieveAsync(msg, lower);

        RagResult result;
        if (!found)
        {
            result = Fallback();               // off-topic → fallback, no LLM cost
        }
        else
        {
            var chat = kernel.Services.GetService<IChatCompletionService>();
            if (chat is not null)
            {
                // 5. Augment + generate (grounded).
                var prompt = $"{SystemPrompt}\n\nCONTEXT:\n{context}\n\nQUESTION: {msg}\n\nANSWER:";
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    var r = await kernel.InvokePromptAsync(prompt, cancellationToken: cts.Token);
                    result = new RagResult(r.ToString().Trim(), true, sources, Suggestions());
                }
                catch
                {
                    result = new RagResult(context, true, sources, Suggestions()); // degrade to KB answer
                }
            }
            else
            {
                // No LLM key → return the grounded KB answer directly.
                result = new RagResult(context, true, sources, Suggestions());
            }
        }

        cache.Set(cacheKey, result, TimeSpan.FromHours(1));
        return result;
    }

    // ── Retrieval: Tier A (embeddings + Supabase) → Tier B (local cosine) ──────
    private async Task<(string Context, List<string> Sources, bool Found)> RetrieveAsync(string message, string lower)
    {
        if (embedder.IsConfigured && supabase.IsConfigured)
        {
            var qv = await embedder.EmbedAsync(message);
            if (qv is not null)
            {
                var hits = await supabase.SearchAsync(qv, topK: 3, threshold: LiveThreshold);
                if (hits.Count > 0)
                {
                    var ctx = string.Join("\n---\n", hits.Select(h => h.Content));
                    var src = hits.Select(h => h.Topic).Where(t => t.Length > 0).Distinct().ToList();
                    return (ctx, src, true);
                }
                return ("", new(), false);
            }
        }

        // Tier B — local bag-of-words cosine over the SQL Server KB.
        var kbVectors = await LocalIndexAsync();
        var match = local.Search(lower, kbVectors, LocalThreshold);
        if (match is not null)
            return (match.Value.Entry.Answer, new List<string> { match.Value.Entry.Topic }, true);

        return ("", new(), false);
    }

    private async Task<IReadOnlyList<(KnowledgeBase Entry, Dictionary<string, int> Vector)>> LocalIndexAsync() =>
        (await cache.GetOrCreateAsync("kb_vectors", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            var all = await db.KnowledgeBase.AsNoTracking().ToListAsync();
            return all.Select(e =>
                (Entry: e, Vector: local.Vectorize($"{e.Keywords} {e.Question} {e.Answer}"))).ToList();
        }))!;

    // ── VC-number account lookup ──────────────────────────────────────────────
    private async Task<RagResult> VcLookupAsync(string vc)
    {
        var subscriber = await db.Subscribers.AsNoTracking().FirstOrDefaultAsync(s => s.VcNumber == vc);
        if (subscriber is null)
            return new RagResult(
                $"❌ VC number '{vc}' was not found. Please check the 11-digit number on the smart card in your set-top box.",
                true, new List<string> { "account" }, Suggestions());

        var sub = await db.Subscriptions.AsNoTracking().Include(s => s.Plan)
            .Where(s => s.SubscriberId == subscriber.Id)
            .OrderByDescending(s => s.ExpiryDate)
            .FirstOrDefaultAsync();

        if (sub?.Plan is null)
            return new RagResult($"✅ VC {vc} — {subscriber.FullName}\nNo active plan found. Please recharge to start a pack.",
                true, new List<string> { "account" }, Suggestions());

        var statusLine = sub.Status switch
        {
            "Expired"  => $"🔴 EXPIRED {Math.Abs(sub.DaysRemaining)} day(s) ago — please recharge to resume service.",
            "Expiring" => $"🟠 Expiring in {sub.DaysRemaining} day(s) — recharge soon to avoid interruption.",
            _          => $"🟢 Active — {sub.DaysRemaining} day(s) remaining."
        };
        var reply = $"✅ VC {vc} — {subscriber.FullName}\n" +
                    $"• Plan: {sub.Plan.Name} (₹{sub.Plan.MonthlyPrice:0})\n" +
                    $"• Valid till: {sub.ExpiryDate:dd MMM yyyy}\n" +
                    $"• Status: {statusLine}";
        return new RagResult(reply, true, new List<string> { "account" }, Suggestions());
    }

    private static string? QuickIntent(string lower)
    {
        var greet = new[] { "hi ", "hi!", "hi,", "hey", "hello", "good morning", "good evening", "good afternoon" };
        if (greet.Any(lower.StartsWith) || lower == "hi")
            return "Hello! Welcome to DishTV / Watcho support. 📺 How can I help you today?\n\n" +
                   "I can help with plans & prices, recharge & expiry, NCF / HD charges, and KYC.";
        if (lower.Contains("thank")) return "You're welcome! Enjoy your entertainment with DishTV. 😊";
        if (lower.Contains("bye") || lower.Contains("goodbye")) return "Goodbye! Recharge on time and keep watching. 👋";
        return null;
    }

    private RagResult Fallback() => new(
        "I couldn't find that in our DishTV information. I can help with plans, recharge, plan expiry, " +
        "NCF & HD charges, discounts and KYC. For anything else, please call support on 1800-200-8080.",
        false, new(), Suggestions());

    private List<string> Suggestions() =>
        cache.GetOrCreate("kb_suggestions", e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return db.KnowledgeBase.AsNoTracking().OrderBy(k => k.Id).Select(k => k.Question).Take(5).ToList();
        })!;

    private static string Normalize(string s) =>
        Regex.Replace(s, @"[^\w\s]", "").Replace("  ", " ").Trim();
}

public record ChatHistory(string Role, string Content);
public record ChatRequest(string Message, List<ChatHistory> History);
public record RagResult(string Reply, bool Grounded, List<string> Sources, List<string> Suggestions);
