using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using PhoenixWatcho.Data;
using PhoenixWatcho.Models;
using PhoenixWatcho.Services;

namespace PhoenixWatcho.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController(IConfiguration config, AppDbContext db, Kernel kernel,
                            RagService rag, EmbeddingService embedder,
                            SupabaseVectorService supabase) : ControllerBase
{
    [HttpGet("diagnose")]
    public IActionResult Diagnose()
    {
        var key = config["OpenAI:Key"] ?? "(missing)";
        var skEnabled = kernel.Services.GetService<IChatCompletionService>() != null;
        return Ok(new
        {
            hasChatKey       = key.StartsWith("sk-") && key.Length > 20,
            chatModel        = config["OpenAI:Model"] ?? "(missing)",
            skEnabled,
            embeddingsReady  = embedder.IsConfigured,
            embeddingModel   = config["OpenAI:EmbeddingModel"] ?? "(missing)",
            supabaseReady    = supabase.IsConfigured,
            retrievalMode    = embedder.IsConfigured && supabase.IsConfigured
                                 ? "Supabase pgvector (real embeddings)"
                                 : "Local cosine similarity (fallback)"
        });
    }

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest("Message is required.");

        var result = await rag.AnswerAsync(request.Message, request.History);

        // Chat analytics — log every conversation.
        db.ChatLogs.Add(new ChatLog
        {
            UserMessage = request.Message,
            BotReply    = result.Reply,
            Topic       = result.Sources.FirstOrDefault() ?? "general",
            CreatedAt   = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        return Ok(new
        {
            reply       = result.Reply,
            grounded    = result.Grounded,
            sources     = result.Sources,
            suggestions = result.Suggestions
        });
    }
}
