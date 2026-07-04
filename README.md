# Phoenix-Watcho — Consumer Portal with AI Chatbot

A full-stack consumer portal with a **live AI-powered chatbot** built using **RAG (Retrieval Augmented Generation)** — React.js 18 frontend, ASP.NET Core 9 Web API backend, SQL Server database, and **Microsoft Semantic Kernel** with Azure OpenAI / Google Gemini.

## Features

### AI Chatbot (RAG Pipeline)
- **Retrieve** — knowledge-base search over SQL Server with relevance scoring (cosine-similarity-style ranking)
- **Augment** — retrieved context injected into structured prompts
- **Generate** — natural responses via Semantic Kernel `InvokePromptAsync` (Azure OpenAI GPT-4o-mini / Gemini)
- **Hallucination prevention** — responses grounded only to verified knowledge-base content
- **AI chat analytics** — every conversation logged with automatic topic detection
- **Dynamic knowledge base** — admin CRUD with instant cache invalidation, zero redeployment
- Real-time registration check — email detection with live database lookup
- Context-aware conversation — remembers history for follow-up answers

### Consumer Portal
- Registration with full validation (alphanumeric password, SHA256 hashing)
- Document upload (JPG/PNG/PDF, max 5MB) with type/size validation
- Login + personal dashboard with document verification status
- Admin panel — student management, document approve/reject, chat analytics dashboard, knowledge-base management

### Performance
- `IMemoryCache` (1-hour TTL) for knowledge-base retrieval — zero DB hits per chat message
- `AsNoTracking()` read queries, response compression
- Fast-path intent handlers answer ~90% of common questions without any AI API cost

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | React.js 18, Vite |
| Backend | ASP.NET Core 9 Web API, C# |
| AI | Microsoft Semantic Kernel, Azure OpenAI (GPT-4o-mini) / Google Gemini |
| Database | SQL Server, EF Core 9 Code First + Migrations |
| Caching | IMemoryCache |

## Getting Started

```bash
# 1. Update the connection string in appsettings.json

# 2. Create the database
dotnet ef database update

# 3. Install frontend dependencies
cd ClientApp
npm install
npm run dev

# 4. Run the backend (separate terminal, project root)
dotnet run
```

Open `http://localhost:5173` — the chatbot is the 💬 button at the bottom right.

To enable AI-generated responses, add your OpenAI key in `appsettings.json`:
```json
"OpenAI": { "Key": "sk-...", "Model": "gpt-4o-mini" }
```
Without a key, the chatbot still works — it returns knowledge-base answers directly.

## Architecture

```
React.js 18 (Vite)
      │  fetch /api/*
ASP.NET Core 9 Web API
      │
      ├── ChatController      → RAG pipeline (Retrieve → Augment → Generate)
      ├── StudentController   → registration / login (SHA256)
      ├── DocumentController  → file upload + validation
      └── AdminController     → analytics, KB CRUD, document approval
      │
SQL Server (EF Core 9 Code First)
      ├── Students
      ├── StudentDocuments
      ├── KnowledgeBase       → RAG source of truth
      └── ChatLogs            → AI analytics
```
