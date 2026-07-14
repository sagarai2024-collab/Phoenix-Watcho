using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using StudentLearningPortal.Data;
using StudentLearningPortal.Models;
using StudentLearningPortal.Services;

namespace StudentLearningPortal.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController(IConfiguration config, AppDbContext db, Kernel kernel,
                            IMemoryCache cache, VectorSearchService vectorSearch) : ControllerBase
{
    private const string SystemPrompt = """
        You are a helpful assistant for Greenland University's student registration portal.
        Answer questions only using the context provided. Be friendly and concise.
        """;

    [HttpGet("diagnose")]
    public IActionResult Diagnose()
    {
        var key = config["OpenAI:Key"] ?? "(missing)";
        var skEnabled = kernel.Services.GetService<IChatCompletionService>() != null;
        return Ok(new {
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

        // Log every conversation to the database for analytics
        var log = new ChatLog
        {
            UserMessage = request.Message,
            BotReply    = reply,
            Topic       = DetectTopic(request.Message.ToLower()),
            CreatedAt   = DateTime.UtcNow
        };
        db.ChatLogs.Add(log);
        await db.SaveChangesAsync();

        return Ok(new { reply });
    }

    private async Task<string> GetReplyAsync(ChatRequest request)
    {
        var msg = request.Message.ToLower().Trim();

        // ── 1. EMAIL CHECK ────────────────────────────────────────────────────
        var emailMatch = Regex.Match(request.Message, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");
        if (emailMatch.Success)
        {
            var email   = emailMatch.Value.ToLower();
            var student = await db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.Email == email);
            if (student != null)
                return $"✅ '{email}' is already registered!\n" +
                       $"• Name: {student.FullName}\n• Course: {student.Course}\n" +
                       $"• Registered: {student.RegisteredAt:dd MMM yyyy}\n" +
                       $"Please click Login to sign in.";
            else
                return $"❌ '{email}' is not found in our records.\n\n" +
                       $"This could mean:\n" +
                       $"• Wrong email address — please check the spelling\n" +
                       $"• Not registered yet — click Register to create your account\n\n" +
                       $"Please verify your email and try again.";
        }

        // ── 2. GREETINGS ──────────────────────────────────────────────────────
        var greetingWords = new[] { "hi ", "hi!", "hi,", "hey", "hello", "good morning", "good evening", "good afternoon" };
        bool isGreeting   = greetingWords.Any(g => msg.StartsWith(g)) || msg == "hi"
                            || (msg.Split(' ').Length == 2 && msg.EndsWith(" here"));

        if (isGreeting)
        {
            var words = request.Message.Trim().Split(' ');
            var name  = words.Length > 1 ? words[1].Trim(',', '!', '.') : words[0].Trim(',', '!', '.');
            if (msg.EndsWith(" here")) name = words[0].Trim(',', '!', '.');
            var greet = name.Length > 1 ? $"Hi {name}!" : "Hello!";
            return $"{greet} Welcome to Greenland University. How can I help you today?\n\n" +
                   "I can assist with:\n• Courses & eligibility\n• Registration process\n• Fees & documents\n• Contact info";
        }

        // ── 3. THANKS & BYE ───────────────────────────────────────────────────
        if (msg.Contains("thank"))
            return "You're welcome! Best of luck with your registration at Greenland University. 😊";

        if (msg.Contains("bye") || msg.Contains("goodbye") || msg.Contains("see you"))
            return "Goodbye! Feel free to come back anytime. Good luck! 👋";

        // ── 4. ACKNOWLEDGMENTS ────────────────────────────────────────────────
        if (msg == "ok" || msg == "okay" || msg == "got it" || msg == "fine" ||
            msg == "noted" || msg == "alright" || msg == "great" || msg == "nice")
            return "Got it! 😊 Feel free to ask if you have any more questions.";

        // ── 5. YES — context aware ────────────────────────────────────────────
        bool isYes = msg.StartsWith("yes") || msg.StartsWith("yeah") || msg.StartsWith("yep") ||
                     msg.StartsWith("sure") || msg.StartsWith("please") || msg.StartsWith("pls");
        if (isYes)
        {
            var lastBotMsg = request.History.LastOrDefault(h => h.Role == "assistant")?.Content?.ToLower() ?? "";
            if (lastBotMsg.Contains("contact"))
                return "📞 Contact Details:\n• Email: admissions@greenlanduniversity.edu\n• Office hours: Mon–Fri, 9 AM to 5 PM";
            if (lastBotMsg.Contains("course") || lastBotMsg.Contains("program"))
                return "📚 Available Courses:\n• B.Tech CS\n• B.Tech ECE\n• B.Tech ME\n• BCA\n• BBA\n• MBA";
            if (lastBotMsg.Contains("fee") || lastBotMsg.Contains("eligib"))
                return "💰 Fees & Eligibility:\n• All programs: ₹900\n• B.Tech: 60% in 12th (PCM)\n• BCA: 55% in 12th (Maths)\n• BBA: 50% in 12th\n• MBA: Graduation required";
            if (lastBotMsg.Contains("registr") || lastBotMsg.Contains("process") || lastBotMsg.Contains("step"))
                return "📝 Registration Steps:\n1. Click Register tab\n2. Fill your details\n3. Upload documents\n4. Pay ₹900 fee\n5. Done!";
            if (lastBotMsg.Contains("document"))
                return "📄 Required Documents:\n1) 10th Marksheet\n2) 12th Marksheet\n3) Passport Photo\n4) Aadhaar Card";
            return "📋 Eligibility:\n• B.Tech: 60% in 12th (PCM)\n• BCA: 55% in 12th\n• BBA: 50% in 12th\n• MBA: Graduation required\n\n" +
                   "📞 Contact: admissions@greenlanduniversity.edu | Mon–Fri 9AM–5PM";
        }

        if (msg.StartsWith("no") || msg.StartsWith("nope") || msg.StartsWith("nah"))
            return "No problem! Feel free to ask anytime. 😊";

        // ── 6. SHOW ALL INFO ──────────────────────────────────────────────────
        if (msg.Contains("all info") || msg.Contains("all detail") || msg.Contains("show detail") ||
            msg.Contains("give info") || msg.Contains("full info") || msg.Contains("everything") ||
            msg.Contains("tell me all") || msg.Contains("tell me about"))
            return "📚 Courses: B.Tech (CS, ECE, ME), BCA, BBA, MBA\n\n" +
                   "💰 Fee: ₹900 for all programs\n\n" +
                   "📋 Eligibility:\n• B.Tech: 60% in 12th (PCM)\n• BCA: 55% in 12th\n• BBA: 50% in 12th\n• MBA: Graduation required\n\n" +
                   "⏰ Deadline: June 30, 2025\n\n" +
                   "📄 Documents: 10th & 12th Marksheet, Passport Photo, Aadhaar Card\n\n" +
                   "📞 Contact: admissions@greenlanduniversity.edu | Mon–Fri 9AM–5PM";

        // ── 7. ALREADY REGISTERED CHECK ──────────────────────────────────────
        if (msg.Contains("already register") || msg.Contains("i am register") ||
            msg.Contains("am i register")    || msg.Contains("i have register") ||
            (msg.Contains("register") && (msg.Contains("am") || msg.Contains("already") || msg.Contains("check"))))
            return "To check your registration, share your email address here and I will check instantly!";

        // ── 8. SPECIFIC TOPIC KEYWORDS ────────────────────────────────────────
        if (msg.Contains("registration process") || msg.Contains("how to register") ||
            msg.Contains("how do i register")    || msg.Contains("steps to register") ||
            msg.Contains("step"))
            return "📝 Registration Steps:\n1. Click the Register tab\n2. Fill in your details\n3. Upload required documents\n4. Pay ₹900 fee online\n5. Registration complete!";

        if (msg.Contains("contact") || msg.Contains("contact info") || msg.Contains("office") ||
            (msg.Contains("email") && !emailMatch.Success))
            return "📞 Contact Details:\n• Email: admissions@greenlanduniversity.edu\n• Office hours: Mon–Fri, 9 AM to 5 PM";

        if (msg.Contains("document") || msg.Contains("upload") || msg.Contains("certificate") || msg.Contains("paper"))
            return "📄 Required Documents:\n1) 10th Marksheet\n2) 12th Marksheet\n3) Passport Photo\n4) Aadhaar Card\n\nFormats: JPG, PNG, PDF (max 5MB)";

        if (msg.Contains("eligib") || msg.Contains("qualify") || msg.Contains("marks") ||
            msg.Contains("percentage") || msg.Contains("%"))
            return "📋 Eligibility Criteria:\n• B.Tech: 60% in 12th with PCM\n• BCA: 55% in 12th with Maths\n• BBA: 50% in 12th (any stream)\n• MBA: Graduation in any stream";

        if (msg.Contains("fee") || msg.Contains("cost") || msg.Contains("price") ||
            msg.Contains("pay") || msg.Contains("amount"))
            return "💰 Registration Fee: ₹900 for all programs (B.Tech, BCA, BBA, MBA)";

        if (msg.Contains("course") || msg.Contains("program") || msg.Contains("offer") || msg.Contains("available"))
            return "📚 Available Courses:\n• B.Tech CS\n• B.Tech ECE\n• B.Tech ME\n• BCA\n• BBA\n• MBA";

        if (msg.Contains("deadline") || msg.Contains("last date"))
            return "⏰ Registration Deadline: June 30, 2025. Apply soon!";

        if (msg.Contains("duration") || msg.Contains("how long") ||
            (msg.Contains("year") && !msg.Contains("register")))
            return "🗓 Course Duration:\n• B.Tech: 4 years\n• MBA: 2 years\n• BCA / BBA: 3 years";

        // ── 9. SK RAG — RETRIEVE → AUGMENT → GENERATE ────────────────────────
        var context = await RetrieveAsync(msg);

        if (context != null)
        {
            var prompt = $"{SystemPrompt}\n\nContext:\n{context}\n\nStudent question: {request.Message}\n\nAnswer:";

            var chatService = kernel.Services.GetService<IChatCompletionService>();
            if (chatService != null)
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    var result = await kernel.InvokePromptAsync(prompt, cancellationToken: cts.Token);
                    return result.ToString();
                }
                catch (Exception ex) { return $"AI Error: {ex.Message}"; }
            }

            return context;
        }

        // ── 10. DEFAULT ───────────────────────────────────────────────────────
        return "I can help you with:\n• Courses offered\n• Fees & eligibility\n• Registration process\n• Documents needed\n• Contact info\n\nWhat would you like to know?";
    }

    // HYBRID RAG RETRIEVAL — vector search (cosine similarity) first, keyword fallback.
    // KB entries are vectorized ONCE and cached 1 hour — like a vector DB index in memory.
    private async Task<string?> RetrieveAsync(string query)
    {
        // Load KB and pre-compute its word-frequency vectors (the "index")
        var kbVectors = await cache.GetOrCreateAsync("kb_vectors", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            var all = await db.KnowledgeBase.AsNoTracking().ToListAsync();
            return all.Select(e =>
                (Entry: e, Vector: vectorSearch.Vectorize($"{e.Keywords} {e.Question} {e.Answer}")))
                .ToList();
        });

        // ── TIER 1: VECTOR SEARCH — true cosine similarity cos(θ) = A·B / (|A|·|B|)
        var match = vectorSearch.Search(query, kbVectors!, threshold: 0.15);
        if (match != null) return match.Value.Entry.Answer;

        // ── TIER 2: KEYWORD FALLBACK — catches exact terms vector search may miss
        var queryWords = query
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim('?', '.', '!', ','))
            .Where(w => w.Length > 2)
            .ToList();

        if (!queryWords.Any()) return null;

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
        if (msg.Contains("fee") || msg.Contains("cost") || msg.Contains("pay"))    return "fee";
        if (msg.Contains("course") || msg.Contains("program"))                      return "course";
        if (msg.Contains("eligib") || msg.Contains("qualify") || msg.Contains("%")) return "eligibility";
        if (msg.Contains("document") || msg.Contains("upload"))                     return "document";
        if (msg.Contains("register") || msg.Contains("step"))                       return "registration";
        if (msg.Contains("contact") || msg.Contains("office"))                      return "contact";
        if (msg.Contains("deadline") || msg.Contains("last date"))                  return "deadline";
        if (msg.Contains("duration") || msg.Contains("year"))                       return "duration";
        return "general";
    }
}

public record ChatHistory(string Role, string Content);
public record ChatRequest(string Message, List<ChatHistory> History);
