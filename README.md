# Lore Forge

Personal writing assistant. Helps with creative writing ideas by learning from your personal logbook of games, books, movies, and shows.

The AI agent is named **Sibila** — a creative muse that draws on your logbook and world notes to help you develop your story.

> Lore Forge does **not** write your story — it helps you think through it.

---

## What it does

You feed it your personal reactions and notes about works you've experienced. It stores and embeds that content, then an AI agent uses it as a knowledge base to help you develop your own original story — characters, worldbuilding, plot structure, themes.

---

## Tech stack

| Concern | Choice |
|---|---|
| API | .NET Minimal API, Vertical Slice (no MediatR) |
| Frontend | Blazor WASM |
| AI | AWS Bedrock — Nova Micro + Titan Embeddings v2 |
| Vector store | pgvector on Postgres |
| Conversation store | DynamoDB |
| File storage | S3 |
| Logging | Serilog → Console, File, Seq |

---

## Solution structure

```
LoreForge.sln
├── src/
│   ├── LoreForge.Api            # Startup, routing, all vertical slices
│   │   ├── Extensions/          # IEndpoint, EndpointExtensions
│   │   └── Features/
│   │       ├── Logbook/         # AddWork, GetWorks, DeleteWork, AddJournalEntry, GetJournalEntries
│   │       ├── Agent/           # Chat, GetHistory, ClearHistory
│   │       └── WorldNotes/      # UpsertWorldNote, GetWorldNotes, GetWorldNoteById, DeleteWorldNote
│   ├── LoreForge.Core           # Domain entities + port interfaces (no external deps)
│   │   ├── Entities/
│   │   ├── Errors/              # Named error factories per entity
│   │   ├── Filtering/           # WorkFilter, PaginationParams
│   │   ├── Ports/               # IEmbeddingService, IAgentService, IEndpoint, etc.
│   │   └── Primitives/          # Result<T>, Error, PagedResult<T>
│   ├── LoreForge.Contracts      # Shared request/response DTOs (referenced by Api + Web)
│   │   ├── Common/              # PagedResult
│   │   ├── Logbook/             # WorkSummary, JournalEntrySummary
│   │   └── WorldNotes/          # WorldNoteSummary
│   ├── LoreForge.Infrastructure # AWS Bedrock, pgvector, DynamoDB integrations
│   │   ├── Bedrock/
│   │   │   └── AgentTools/
│   │   └── Persistence/         # DbContext, migrations, QueryableExtensions
│   └── LoreForge.Web            # Blazor WASM frontend
│       ├── Layout/              # MainLayout
│       ├── Pages/
│       │   ├── Works/           # Works.razor, WorkDetail.razor
│       │   ├── Logbook/         # JournalEntries.razor
│       │   ├── WorldNotes/      # WorldNotes.razor, WorldNoteDetail.razor
│       │   └── Agent/           # Agent.razor (Sibila chat)
│       └── Shared/
└── tests/
    ├── LoreForge.UnitTests
    └── LoreForge.IntegrationTests
```

---

## Agent tools

The AI agent can call these tools against your logbook:

| Tool | Searches | Purpose |
|---|---|---|
| `search_inspiration` | Works + Journal entries | Find logged works matching a creative query |
| `check_world_consistency` | World notes | Detect contradictions in your world notes |
| `suggest_character_arc` | Works + Journal entries | Propose arcs grounded in your logged works |
| `analyze_plot_structure` | World notes + Journal entries | Map your plot against known story structures |
| `find_thematic_connections` | Works + Journal entries | Link themes across your entire logbook |

---

## Prerequisites

- .NET 10 SDK
- Docker (for local Postgres + Seq via docker compose)
- AWS account with Bedrock access (Nova Micro + Titan Embeddings v2)
- DynamoDB table for conversation history
- S3 bucket for journal file uploads

---

## Running locally

Start the infrastructure:

```bash
cd docker
docker compose up -d
```

Run the API:

```bash
dotnet run --project src/LoreForge.Api
```

Run the frontend:

```bash
dotnet run --project src/LoreForge.Web
```

Seq UI is available at `http://localhost:5342`.

---

## Configuration

### API (`src/LoreForge.Api/appsettings.json`)

```json
{
  "ConnectionStrings": {
    "Postgres": "<your-postgres-connection-string>"
  },
  "Cors": {
    "AllowedOrigins": [ "https://your-frontend-url" ]
  },
  "Bedrock": {
    "AgentModelId": "amazon.nova-micro-v1:0",
    "EmbeddingModelId": "amazon.titan-embed-text-v2:0"
  },
  "DynamoDB": {
    "TableName": "<your-table-name>"
  },
  "S3": {
    "BucketName": "<your-bucket-name>"
  },
  "Serilog": {
    "WriteTo": [
      { "Name": "Seq", "Args": { "serverUrl": "<your-seq-url>" } }
    ]
  }
}
```

Development CORS origins are already set in `appsettings.Development.json`. In production, override via environment variable:

```
Cors__AllowedOrigins__0=https://your-frontend-url
```

### Frontend (`src/LoreForge.Web/wwwroot/appsettings.json`)

```json
{
  "ApiBaseUrl": "https://your-api-url"
}
```

---

## Running tests

```bash
dotnet test
```

> Integration tests spin up a Postgres container automatically via Testcontainers — no manual setup required.
