using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace PhoenixWatcho.Services;

/// <summary>
/// Supabase (Postgres + pgvector) vector store accessed over the PostgREST API.
/// Upserts KB document embeddings and runs cosine-similarity search through the
/// SQL function <c>match_documents</c> (see infra/supabase/schema.sql).
/// Embeddings are sent as a pgvector text literal ("[0.1,0.2,...]") which pgvector
/// parses directly — robust across raw REST calls.
/// </summary>
public class SupabaseVectorService(HttpClient http, IConfiguration config)
{
    private readonly string _url   = (config["Supabase:Url"] ?? "").TrimEnd('/');
    private readonly string _key   = config["Supabase:ServiceKey"] ?? "";
    private readonly string _table = config["Supabase:Table"] ?? "kb_documents";

    public bool IsConfigured => _url.Length > 0 && _key.Length > 0;

    private HttpRequestMessage Build(HttpMethod method, string path)
    {
        var req = new HttpRequestMessage(method, $"{_url}{path}");
        req.Headers.TryAddWithoutValidation("apikey", _key);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _key);
        return req;
    }

    private static string ToVectorLiteral(float[] v) =>
        "[" + string.Join(",", v.Select(x => x.ToString(CultureInfo.InvariantCulture))) + "]";

    /// <summary>Upsert KB documents (insert or update by id) with their embeddings.</summary>
    public async Task UpsertAsync(IEnumerable<KbVectorDoc> docs, CancellationToken ct = default)
    {
        if (!IsConfigured) return;

        var payload = docs.Select(d => new
        {
            id        = d.Id,
            topic     = d.Topic,
            content   = d.Content,
            embedding = ToVectorLiteral(d.Embedding)
        });

        using var req = Build(HttpMethod.Post, $"/rest/v1/{_table}?on_conflict=id");
        req.Headers.TryAddWithoutValidation("Prefer", "resolution=merge-duplicates,return=minimal");
        req.Content = JsonContent.Create(payload);

        using var resp = await http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
    }

    /// <summary>Cosine-similarity search. Returns hits scoring ≥ threshold, best first.</summary>
    public async Task<List<RagHit>> SearchAsync(float[] queryEmbedding, int topK, double threshold, CancellationToken ct = default)
    {
        if (!IsConfigured) return new();

        using var req = Build(HttpMethod.Post, "/rest/v1/rpc/match_documents");
        req.Content = JsonContent.Create(new
        {
            query_embedding = ToVectorLiteral(queryEmbedding),
            match_threshold = threshold,
            match_count     = topK
        });

        using var resp = await http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();

        var rows = await resp.Content.ReadFromJsonAsync<List<MatchRow>>(cancellationToken: ct) ?? new();
        return rows.Select(r => new RagHit(r.Id, r.Topic ?? "", r.Content ?? "", r.Similarity)).ToList();
    }

    private record MatchRow(int Id, string? Topic, string? Content, double Similarity);
}

public record KbVectorDoc(int Id, string Topic, string Content, float[] Embedding);
public record RagHit(int Id, string Topic, string Content, double Score);
