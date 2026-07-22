-- ============================================================================
-- Phoenix-Watcho — Supabase vector store schema
-- Run this once in your Supabase project: SQL Editor → paste → Run.
-- Requires the pgvector extension (bundled with Supabase).
-- Embedding dimension 768 matches Google Gemini text-embedding-004.
-- (If you switch to OpenAI text-embedding-3-small, use vector(1536) instead.)
-- ============================================================================

-- 1. Enable pgvector
create extension if not exists vector;

-- 2. Knowledge-base documents table
create table if not exists kb_documents (
    id        integer primary key,           -- mirrors KnowledgeBase.Id from SQL Server
    topic     text,
    content   text not null,
    metadata  jsonb default '{}'::jsonb,
    embedding vector(768) not null
);

-- 3. Approximate-nearest-neighbour index for fast cosine search at scale
create index if not exists kb_documents_embedding_idx
    on kb_documents
    using hnsw (embedding vector_cosine_ops);

-- 4. Cosine-similarity search function.
--    Returns rows scoring >= match_threshold, most similar first.
--    similarity = 1 - cosine_distance, so 1.0 = identical, 0 = unrelated.
create or replace function match_documents (
    query_embedding vector(768),
    match_threshold float default 0.75,
    match_count     int   default 3
)
returns table (
    id         integer,
    topic      text,
    content    text,
    similarity float
)
language sql stable
as $$
    select
        kb_documents.id,
        kb_documents.topic,
        kb_documents.content,
        1 - (kb_documents.embedding <=> query_embedding) as similarity
    from kb_documents
    where 1 - (kb_documents.embedding <=> query_embedding) >= match_threshold
    order by kb_documents.embedding <=> query_embedding
    limit match_count;
$$;

-- After running this, populate the table by calling the app endpoint:
--   POST /api/admin/reindex   (embeds every KB row and upserts here)
