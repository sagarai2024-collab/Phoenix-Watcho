# DishTV Chatbot — RAG Setup (Semantic Kernel + OpenAI + Supabase pgvector)

The chatbot answers subscriber questions with **Retrieval Augmented Generation**:

```
User question
   │
   ├─ intent routing / cache  ──► instant answer (no LLM cost)
   │
   ├─ embed query (OpenAI text-embedding-3-small)
   ├─ cosine search in Supabase pgvector  → top-K relevant KB chunks
   ├─ grounded prompt (answer ONLY from context)  → Semantic Kernel · gpt-4o-mini
   └─ below relevance threshold → fallback UI (suggested questions + support)
```

It works **out of the box with no keys** — it falls back to a local cosine-similarity
search over the SQL Server knowledge base. To run the full live pipeline, add the two
credentials below.

## 1. Create the Supabase vector store
1. Create a free project at <https://supabase.com>.
2. Open **SQL Editor**, paste [`infra/supabase/schema.sql`](../infra/supabase/schema.sql), and **Run**.
   This enables `pgvector`, creates the `kb_documents` table, an HNSW index, and the
   `match_documents` cosine-similarity function.
3. In **Project Settings → API**, copy the **Project URL** and the **service_role** key.

## 2. Add your keys (kept out of git)
Create `appsettings.Development.json` in the project root (already git-ignored):

```json
{
  "OpenAI": {
    "Key": "sk-...your-openai-key...",
    "Model": "gpt-4o-mini",
    "EmbeddingModel": "text-embedding-3-small"
  },
  "Supabase": {
    "Url": "https://YOUR-PROJECT.supabase.co",
    "ServiceKey": "your-service-role-key",
    "Table": "kb_documents"
  }
}
```

## 3. Index the knowledge base
Start the app, then embed every KB row and upsert the vectors into Supabase:

```bash
curl -X POST http://localhost:5000/api/admin/reindex
# → { "indexed": 10, "of": 10 }
```

Verify in Supabase: `select count(*) from kb_documents;`

## 4. Check the mode
```bash
curl http://localhost:5000/api/chat/diagnose
```
`retrievalMode` shows either **"Supabase pgvector (real embeddings)"** (fully live) or
**"Local cosine similarity (fallback)"** (no keys yet).

## Cost control (the ~90% saving)
- **Intent routing** — greetings, VC-number lookups and off-topic questions never call the LLM.
- **Response cache** — identical/repeated questions are served from `IMemoryCache` (1-hour TTL).
- **Relevance threshold** — if nothing scores above the cosine threshold, the bot returns the
  fallback UI instead of paying for a low-quality generation.

## Hallucination prevention
The system prompt instructs the model to answer **only** from the retrieved context and to
defer to support otherwise; retrieved topics are shown to the user as source chips.

## Editing the knowledge base
Admin panel → **Knowledge Base** tab (add/edit/delete). After changes, call
`POST /api/admin/reindex` again to refresh the Supabase vectors.
