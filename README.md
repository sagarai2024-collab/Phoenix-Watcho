# Phoenix-Watcho

A DTH subscriber self-care portal with a RAG support assistant. Subscribers sign in with their 11-digit Viewing Card number to see their plan, days to expiry and an itemized bill, and ask the assistant questions answered from a knowledge base stored in Supabase pgvector.

Built with React 19 (Vite), ASP.NET Core 9 Web API, EF Core 9 on SQL Server and Microsoft Semantic Kernel. Embeddings sit in Supabase pgvector behind an HNSW index and are searched by cosine similarity through a `match_documents` SQL function. The assistant uses Google Gemini when a Gemini key is set, OpenAI otherwise, and returns the best knowledge-base answer directly when neither is configured.

It is a portfolio build modelled on a consumer portal I worked on in the OTT/DTH industry. Plans, prices and subscribers are seeded demo data.

**Status.** Portal (plans, expiry, billing, KYC, admin): working. Assistant: retrieval and generation working. Admin API protected by role-based cookie auth. Account lookup in chat answers only for the signed-in subscriber.

## Features

### Consumer Portal
- **VC-number login** — subscribers sign in with the 11-digit Viewing Card number on their set-top box.
- **Current plan & live expiry status** — Active / Expiring (≤5 days) / Expired, with an on-screen notification banner and a one-tap **Recharge Now**.
- **Itemized billing** — every plan shows a full breakdown: base pack price − promotional discount + **NCF (Network Capacity Fee)** + **HD add-on** + **18% GST** = total payable.
- **Available plans grid** — browse and switch packs (Base ₹199 → Premium HD ₹1199); discounts shown per pack.
- **New connection + KYC** — register a connection (auto-assigned VC number) and upload KYC documents (Aadhaar / Address Proof / Photo).
- **Admin panel** — subscribers, plans, KYC approval, chat logs, and analytics.

### AI Chatbot (RAG pipeline)
- **Retrieve** — vector search using true cosine similarity (`cos θ = A·B / |A|·|B|`) over the knowledge base, with a keyword fallback (`Services/VectorSearchService.cs`).
- **Augment / Generate** — retrieved context injected into a grounded prompt, answered via Semantic Kernel (Azure OpenAI GPT-4o-mini) when a key is configured; otherwise the grounded KB answer is returned directly.
- **VC lookup in chat** — type your VC number and the bot returns your plan and expiry status.
- **Analytics** — every conversation is logged with automatic topic detection.
- **Dynamic knowledge base** — admin CRUD with instant cache invalidation, zero redeploy.

> The chatbot's deeper "consumer" features are an ongoing enhancement; the portal (plans, expiry, billing, KYC, admin) is fully functional.

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | React 19, Vite |
| Backend | ASP.NET Core 9 Web API, C# |
| AI | Microsoft Semantic Kernel, Azure OpenAI (GPT-4o-mini) |
| Database | SQL Server, EF Core 9 Code First + Migrations |
| Caching | IMemoryCache |

## Getting Started

```bash
# 1. Set your SQL Server connection string in appsettings.json

# 2. Install frontend dependencies and start Vite
cd ClientApp
npm install
npm run dev

# 3. Run the backend (separate terminal, project root).
#    Migrations are applied and demo data seeded automatically on startup.
dotnet run
```

Open `http://localhost:5173` (dev) — the chatbot is the 💬 button at the bottom right.

### Demo logins (seeded automatically)

| VC Number | Password | State |
|---|---|---|
| `01234567890` | `Pass123` | Active plan (Sports HD, ~25 days left) |
| `01234500001` | `Pass123` | Expiring (3 days left) |
| `01234500002` | `Pass123` | Expired (recharge banner) |

Admin panel password: `admin123`, checked on the server and set in `appsettings.json` under `Admin:Password`. Change it before deploying anywhere.

To enable AI-generated chatbot responses, add your OpenAI key in `appsettings.json`:
```json
"OpenAI": { "Key": "sk-...", "Model": "gpt-4o-mini" }
```
Without a key the chatbot still works — it returns grounded knowledge-base answers directly.

## Architecture

```
React 19 (Vite)
      │  fetch /api/*
ASP.NET Core 9 Web API
      │
      ├── SubscriberController → VC-number register / login (PasswordHasher, cookie auth)
      ├── PlanController        → plans, expiry status, itemized billing
      ├── DocumentController    → KYC upload + validation
      ├── AdminController       → subscribers, plans, KYC, analytics, KB CRUD
      └── ChatController        → RAG pipeline (Retrieve → Augment → Generate)
      │
      ├── BillingService        → base + NCF + HD + GST − discount
      └── VectorSearchService   → cosine-similarity KB search
      │
SQL Server (EF Core 9 Code First)
      ├── Subscribers
      ├── Plans / Subscriptions
      ├── KycDocuments
      ├── KnowledgeBase         → RAG source of truth
      └── ChatLogs              → AI analytics
```
