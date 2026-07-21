using PhoenixWatcho.Models;

namespace PhoenixWatcho.Services;

/// <summary>
/// Vector search over the Knowledge Base using bag-of-words vectors
/// and true cosine similarity: cos(θ) = A·B / (|A| × |B|).
/// Same formula used by embedding-based vector DBs (Qdrant, Pinecone) —
/// here the vectors are word frequencies; the upgrade path is swapping
/// them for embedding vectors without changing the search logic.
/// </summary>
public class VectorSearchService
{
    // Words too common to carry meaning — excluded from vectors
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "is", "a", "an", "of", "to", "in", "for", "on", "and",
        "or", "do", "does", "what", "how", "can", "i", "my", "me", "you"
    };

    /// <summary>Convert text to a word-frequency vector: word → count.</summary>
    public Dictionary<string, int> Vectorize(string text)
    {
        return text.ToLower()
            .Split(new[] { ' ', '?', '.', '!', ',', ':', ';', '\n', '\r', '\t' },
                   StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2 && !StopWords.Contains(w))
            .Select(Stem)
            .GroupBy(w => w)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>Light stemming so "channels" and "channel" become the same token.</summary>
    private static string Stem(string word)
    {
        if (word.EndsWith("ies") && word.Length > 4) return word[..^3] + "y";
        if (word.EndsWith("s") && word.Length > 3)   return word[..^1];
        return word;
    }

    /// <summary>
    /// True cosine similarity between two word-frequency vectors.
    /// Returns 0..1 — 1 = identical meaning-words, 0 = nothing shared.
    /// </summary>
    public double CosineSimilarity(Dictionary<string, int> a, Dictionary<string, int> b)
    {
        if (a.Count == 0 || b.Count == 0) return 0;

        double dot = a.Keys.Intersect(b.Keys)
                      .Sum(w => (double)a[w] * b[w]);

        double magA = Math.Sqrt(a.Values.Sum(v => (double)v * v));
        double magB = Math.Sqrt(b.Values.Sum(v => (double)v * v));

        return dot / (magA * magB);   // cos(θ) = A·B / (|A| × |B|)
    }

    /// <summary>
    /// Search pre-vectorized KB entries for the best match to the query.
    /// Returns the entry and its score, or null when below the threshold.
    /// </summary>
    public (KnowledgeBase Entry, double Score)? Search(
        string query,
        IReadOnlyList<(KnowledgeBase Entry, Dictionary<string, int> Vector)> kbVectors,
        double threshold = 0.15)
    {
        var queryVector = Vectorize(query);
        if (queryVector.Count == 0) return null;

        (KnowledgeBase Entry, double Score)? best = null;
        foreach (var (entry, vector) in kbVectors)
        {
            var score = CosineSimilarity(queryVector, vector);
            if (best == null || score > best.Value.Score)
                best = (entry, score);
        }

        return best?.Score >= threshold ? best : null;
    }
}
