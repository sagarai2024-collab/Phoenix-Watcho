# DishTV Chatbot — RAG Setup (Semantic Kernel + Gemini + Supabase pgvector)

The chatbot answers subscriber questions with **Retrieval Augmented Generation**:

```
User question
   │
   ├─ intent routing / cache  ──► instant answer (no LLM cost)
   │
   ├─ embed query (Gemini text-embedding-004, 768-dim)
   ├─ cosine search in Supabase pgvector  → top-K relevant KB chunks
   ├─ grounded prompt (answer ONLY from context)  → Semantic Kernel · Gemini
   └─ below relevance threshold → fallback UI (suggested questions + support)
```

It works **out of the box with no keys** — it falls back to a local cosine-similarity
search over the SQL Server knowledge base. To run the full live pipeline, add the two
free credentials below.

> **Provider:** default is **Google Gemini** (free tier, no card). The code also supports
> OpenAI automatically — if you set an `OpenAI:Key` instead, it uses text-embedding-3-small
> (1536-dim) + gpt-4o-mini. If you go the OpenAI route, change `vector(768)` → `vector(1536)`
> in `infra/supabase/schema.sql` before running it.

## 1. Get a free Google Gemini API key (no card)
1. Go to **<https://aistudio.google.com/apikey>** and sign in with a Google account.
2. Click **Create API key** → copy it (starts with `AIza...`).
3. Put it in `appsettings.Development.json` → `Gemini.Key`.

## 2. Create the Supabase vector store (free)
1. Create a project at **<https://supabase.com/dashboard>** (New project → name → password → region).
2. Open **SQL Editor → New query**, paste [`infra/supabase/schema.sql`](../infra/supabase/schema.sql), **Run**.
   (Enables `pgvector`, creates `kb_documents` with `vector(768)`, an HNSW index, and the
   `match_documents` cosine function.)
3. **Settings → API**: copy the **Project URL** and the **service_role** key.

## 3. Fill in `appsettings.Development.json` (git-ignored)
```json
{
  "Gemini": {
    "Key": "AIza...your-gemini-key...",
    "Model": "gemini-2.0-flash",
    "EmbeddingModel": "text-embedding-004"
  },
  "Supabase": {
    "Url": "https://YOUR-PROJECT.supabase.co",
    "ServiceKey": "your-service-role-key",
    "Table": "kb_documents"
  }
}
```

## 4. Index the knowledge base
Start the app, then embed every KB row and upsert the vectors into Supabase:
```bash
curl -X POST http://localhost:5000/api/admin/reindex
# → { "indexed": 10, "of": 10 }
```
Verify in Supabase: `select count(*) from kb_documents;`

## 5. Check the mode
```bash
curl http://localhost:5000/api/chat/diagnose
```
`retrievalMode` flips from **"Local cosine similarity (fallback)"** to
**"Supabase pgvector (Gemini embeddings)"**, and `embeddingProvider` shows `Gemini`.

## Cost control (the ~90% saving)
- **Intent routing** — greetings, VC-number lookups and off-topic questions never call the LLM.
- **Response cache** — identical/repeated questions are served from `IMemoryCache` (1-hour TTL).
- **Relevance threshold** — nothing above the cosine threshold → fallback UI, no paid generation.

## Hallucination prevention
The system prompt instructs the model to answer **only** from the retrieved context and to
defer to support otherwise; retrieved topics are shown to the user as source chips.

## Editing the knowledge base
Admin panel → **Knowledge Base** tab (add/edit/delete). After changes, call
`POST /api/admin/reindex` again to refresh the Supabase vectors.
