---
applyTo: "**"
---

This is a .NET Minimal API project using Vertical Slice architecture. Review with the following conventions in mind.

## Vertical slices

Each feature lives in a single file with three parts in this order:
1. Request/command record (e.g., `AddWorkRequest`)
2. Handler class implementing `IEndpoint` with a constructor, `HandleAsync`, and `MapEndpoint`
3. `MapEndpoint` is a static method that registers the route and scopes the handler

Do not create separate files for requests, handlers, or DTOs — keep the slice self-contained.

## Naming

| Thing | Pattern | Example |
|---|---|---|
| Handler class | `{VerbNoun}Handler` | `AddWorkHandler` |
| Request record | `{VerbNoun}Request` | `AddWorkRequest` |
| Response DTO | `{Entity}Detail` or `{Entity}Summary` | `WorkDetail` |
| Error statics | `{Entity}Errors` | `WorkErrors` |
| Test methods | `Should_[Result]_When_[Context]` | `Should_Return404_When_WorkNotFound` |

## Result pattern

- Return `Result<T>` from handlers, never throw exceptions for domain errors.
- Define errors as statics on `{Entity}Errors` using `Error(code, description, ErrorType)`.
- Map to HTTP at the endpoint boundary with `.ToHttpResult()`.
- Use `ErrorType.Validation`, `ErrorType.NotFound`, or `ErrorType.Conflict` — never raw strings at the call site.

## EF Core

- Inject `DbContext` directly into handlers — no repository layer.
- Use `.AsNoTracking()` for read-only queries.
- Never put `.FindAsync()`, `.FirstOrDefaultAsync()`, or any async call directly inside an `if` — store the result in a variable first.

## Domain model (DDD)

- Entities own their business logic — validation and state transitions live on the entity, not in the handler.
- Use static `Create(...)` factory methods on entities that return `Result<T>` to enforce invariants on construction.
- Use mutation methods (e.g., `Work.UpdateNotes(...)`, `Work.Complete()`) for state changes — do not set properties directly from handlers.
- Properties should have private setters; public setters are a red flag.
- Handlers are thin orchestrators: load entity → call entity method → persist → return result.

## Validation

- Validation lives on the entity (via factory/mutation methods), not in the handler.
- Return `Result.Failure<T>()` from entity methods on invalid input — no exceptions.
- Do not use FluentValidation or middleware-based validation.

## Dependency injection

- All `IEndpoint` implementations are auto-registered via `AddEndpointHandlers` — no manual registration needed.
- Infrastructure services (AWS, EF, etc.) are registered manually in `Program.cs`.
- Private methods must always be at the bottom of the class.

## Project boundaries

- `Core` must have zero external dependencies — no AWS SDK, no EF Core, no Npgsql.
- Ports/interfaces for external services live in `Core/Ports`.
- Cross-boundary DTOs live in `LoreForge.Contracts`.
- Entities in `Core/Entities` own their business logic — rich domain model, not plain data bags.
- Model IDs and configuration values must come from injected config, never hardcoded.

## Testing

- Prefer integration tests over unit tests; write unit tests only for logic-heavy, isolated code.
- Integration tests inherit `BaseIntegrationTest`, use `[Collection(PostgresCollection.Name)]`.
- Clean the database between tests via `Context.{Entity}.ExecuteDelete()`.
- Use `NSubstitute` for mocks with `.Received()` for verification.
- Do not mock the database in integration tests.
- Flag any missing test coverage for new features or bug fixes.

## Security & general

- Flag security issues: SQL injection, unvalidated input at system boundaries, exposed secrets.
- No secrets or credentials in code.
- Keep scope tight — do not suggest speculative abstractions or features not requested.
