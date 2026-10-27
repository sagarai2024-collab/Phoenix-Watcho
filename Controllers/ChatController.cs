using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using PhoenixWatcho.Data;
using PhoenixWatcho.Models;
using PhoenixWatcho.Services;

namespace PhoenixWatcho.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController(AppDbContext db, Kernel kernel,
                            RagService rag, EmbeddingService embedder,
                            SupabaseVectorService supabase) : ControllerBase
{
    [HttpGet("diagnose")]
    public IActionResult Diagnose()
    {
        var skEnabled = kernel.Services.GetService<IChatCompletionService>() != null;
        return Ok(new
        {
            llmEnabled        = skEnabled,
            embeddingProvider = embedder.Provider,          // Gemini / OpenAI / None
            embeddingDims     = embedder.Dimensions,        // 768 (Gemini) or 1536 (OpenAI)
            embeddingsReady   = embedder.IsConfigured,
            supabaseReady     = supabase.IsConfigured,
            retrievalMode     = embedder.IsConfigured && supabase.IsConfigured
                                 ? $"Supabase pgvector ({embedder.Provider} embeddings)"
                                 : "Local cosine similarity (fallback)"
        });
    }

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest("Message is required.");

        // Only the signed-in subscriber's own VC can be looked up in chat.
        var result = await rag.AnswerAsync(request.Message, request.History, User.VcNumber());

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
