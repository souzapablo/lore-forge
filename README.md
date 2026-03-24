# Lore Forge

Personal writing assistant API. Helps with creative writing ideas by learning from your personal logbook of games, books, movies, and shows.

> Lore Forge does **not** write your story — it helps you think through it.

---

## What it does

You feed it your personal reactions and notes about works you've experienced. It stores and embeds that content, then an AI agent uses it as a knowledge base to help you develop your own original story — characters, worldbuilding, plot structure, themes.

---

## Tech stack

| Concern | Choice |
|---|---|
| Framework | .NET Minimal API |
| Architecture | Vertical Slice (no MediatR) |
| AI | AWS Bedrock — Nova Micro + Titan Embeddings v2 |
| Vector store | pgvector on RDS Postgres |
| Conversation store | DynamoDB |
| File storage | S3 |

---

## Solution structure

```
LoreForge.sln
├── src/
│   ├── LoreForge.Api            # Startup, routing, all vertical slices
│   │   └── Features/
│   │       ├── Logbook/         # AddWork, GetWorks, DeleteWork, AddJournalEntry, GetJournalEntries
│   │       ├── Agent/           # Chat, GetHistory, ClearHistory
│   │       └── WorldNotes/      # UpsertNote, GetNotes
│   ├── LoreForge.Core           # Domain entities + port interfaces (no external deps)
│   │   ├── Entities/
│   │   └── Ports/
│   └── LoreForge.Infrastructure # AWS Bedrock, pgvector, DynamoDB integrations
│       ├── Bedrock/
│       │   └── AgentTools/
│       └── Persistence/
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
- PostgreSQL with pgvector extension
- AWS account with Bedrock access (Nova Micro + Titan Embeddings v2)
- DynamoDB table for conversation history
- S3 bucket for journal file uploads

---

## Configuration

```json
{
  "Bedrock": {
    "AgentModelId": "amazon.nova-micro-v1:0",
    "EmbeddingModelId": "amazon.titan-embed-text-v2:0"
  },
  "ConnectionStrings": {
    "Postgres": "<your-postgres-connection-string>"
  },
  "DynamoDB": {
    "TableName": "<your-table-name>"
  },
  "S3": {
    "BucketName": "<your-bucket-name>"
  }
}
```

---

## Running locally

```bash
dotnet restore
dotnet build
dotnet run --project src/LoreForge.Api
```

---

## Running tests

```bash
dotnet test tests/LoreForge.UnitTests
dotnet test tests/LoreForge.IntegrationTests
```
