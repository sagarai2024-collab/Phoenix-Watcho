using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace PhoenixWatcho.Services;

/// <summary>
/// Generates real vector embeddings. Uses Google Gemini (free tier,
/// text-embedding-004 → 768-dim) when a Gemini key is set, otherwise falls
/// back to OpenAI (text-embedding-3-small → 1536-dim). Returns null when
/// neither is configured, which tells the RAG pipeline to use local search.
/// </summary>
public class EmbeddingService(HttpClient http, IConfiguration config)
{
    private readonly string _geminiKey   = config["Gemini:Key"] ?? "";
    private readonly string _geminiModel = config["Gemini:EmbeddingModel"] ?? "text-embedding-004";
    private readonly string _openAiKey   = config["OpenAI:Key"] ?? "";
    private readonly string _openAiModel = config["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";

    private bool GeminiReady => _geminiKey.StartsWith("AIza");
    private bool OpenAiReady => _openAiKey.StartsWith("sk-") && _openAiKey.Length > 20;

    public bool IsConfigured => GeminiReady || OpenAiReady;
    public string Provider   => GeminiReady ? "Gemini" : OpenAiReady ? "OpenAI" : "None";
    public int Dimensions    => GeminiReady ? 768 : 1536;

    public async Task<float[]?> EmbedAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (GeminiReady) return await EmbedGeminiAsync(text, ct);
        if (OpenAiReady) return await EmbedOpenAiAsync(text, ct);
        return null;
    }

    // ── Google Gemini (text-embedding-004, 768-dim) ───────────────────────────
    private async Task<float[]?> EmbedGeminiAsync(string text, CancellationToken ct)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_geminiModel}:embedContent?key={_geminiKey}";
        var payload = new
        {
            model   = $"models/{_geminiModel}",
            content = new { parts = new[] { new { text } } }
        };

        using var resp = await http.PostAsJsonAsync(url, payload, ct);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<GeminiEmbedResponse>(cancellationToken: ct);
        return body?.Embedding?.Values;
    }

    // ── OpenAI (text-embedding-3-small, 1536-dim) ─────────────────────────────
    private async Task<float[]?> EmbedOpenAiAsync(string text, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/embeddings");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiKey);
        req.Content = JsonContent.Create(new { model = _openAiModel, input = text });

        using var resp = await http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<OpenAiEmbedResponse>(cancellationToken: ct);
        return body?.Data?.FirstOrDefault()?.Embedding;
    }

    private record GeminiEmbedResponse(GeminiEmbedding? Embedding);
    private record GeminiEmbedding(float[]? Values);
    private record OpenAiEmbedResponse(List<OpenAiEmbedData>? Data);
    private record OpenAiEmbedData(float[]? Embedding);
}
