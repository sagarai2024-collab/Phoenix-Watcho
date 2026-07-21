using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace PhoenixWatcho.Services;

/// <summary>
/// Generates real vector embeddings via the OpenAI embeddings API
/// (default model: text-embedding-3-small → 1536-dim vectors).
/// Returns null when no API key is configured, which signals the RAG
/// pipeline to fall back to local bag-of-words search.
/// </summary>
public class EmbeddingService(HttpClient http, IConfiguration config)
{
    private readonly string _key   = config["OpenAI:Key"] ?? "";
    private readonly string _model = config["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";

    public bool IsConfigured => _key.StartsWith("sk-") && _key.Length > 20;

    public async Task<float[]?> EmbedAsync(string text, CancellationToken ct = default)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(text)) return null;

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/embeddings");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _key);
        req.Content = JsonContent.Create(new { model = _model, input = text });

        using var resp = await http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: ct);
        return body?.Data?.FirstOrDefault()?.Embedding;
    }

    private record EmbeddingResponse(List<EmbeddingData>? Data);
    private record EmbeddingData(float[]? Embedding);
}
