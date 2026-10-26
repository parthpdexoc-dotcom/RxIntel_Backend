# OrigencePart Engineering Standards

**Reference repository:** `D:\Skythread-EU-Reboot\OrigencePart`  
**Solution:** `Origence.sln` (.NET 8)  
**Purpose:** Canonical standards that RxIntel should follow during refactoring.

---

## 1. Overall Architecture

OrigencePart is a **multi-host modular monolith**:

- Vertical slices per domain: Part, Asset, Aircraft, Master, Psv, Rag
- Shared kernel: `Origence.Generic.*` + `Origence.Logger`
- Layered flow per domain: **API → Services → Repositories → Entities**
- Cross-cutting: Common helpers, Enums, Dtos, Models, Logger

```
API Host (Origence.Parts / Asset / Aircraft / Master / Psv)
  → Domain.Services (+ Worker)
    → Domain.Repositories → Domain.Entities (DbContext)
    → Domain.Dtos (AutoMapper profiles)
    → Generic.Services / Generic.Repositories / Generic.Common
```

**Principle:** Prefer reusable generics in Generic.* over duplicated domain logic. Domain projects stay thin wrappers.

---

## 2. Project & Folder Organization

### Domain project naming

`Origence.{Domain}.{Layer}`

| Layer | Example |
|-------|---------|
| API host | `Origence.Parts` |
| Services | `Origence.Part.Services` |
| Repositories | `Origence.Part.Repositories` |
| Entities | `Origence.Part.Entities` |
| Dtos | `Origence.Part.Dtos` |
| Models | `Origence.Part.Models` |
| Worker | `Origence.Part.Worker` |

### Shared projects

`Origence.Generic.Api`, `.Common`, `.Dtos`, `.Entities`, `.Enums`, `.Model`/`.Models`, `.Repositories`, `.Services`, `Origence.Logger`

### Folder conventions

| Project type | Folders |
|--------------|---------|
| API host | `Controllers/`, `Extentions/` (DI, Swagger), `Validators/`, `Program.cs` |
| Services | `Interfaces/`, `Services/`, optional `Extensions/` |
| Repositories | `Interfaces/`, `Repositories/` |
| Entities | `Entities/`, `DBContext/` |
| Dtos | `Dtos/`, `Mapper/` |
| Common | `Implementations/{Helpers,Responses,Exceptions,...}`, `Extentions/` |

---

## 3. Layer Separation & Dependency Flow

**Allowed (outer → inner):**

1. Presentation (Controllers)
2. Application (Services)
3. Data (Repositories + EF DbContext)
4. Domain contracts (Entities, Enums, base DTO interfaces)
5. Cross-cutting (Common, Logger)

**Standards:**

- Controllers depend on **service interfaces**, not repositories
- Services depend on **repository interfaces** (and Generic context abstractions)
- Repositories wrap EF; thin domain repos inherit `GenericRepository<T,TContext>`
- Prefer constructor DI; register in host `DependencyInjection` extension methods

---

## 4. Design Patterns

| Pattern | Standard usage |
|---------|----------------|
| Repository | `IGenericRepository<T>` + `GenericRepository<T,TContext>`; domain repos are thin |
| Service | `I{Name}Service` / `{Name}Service`; domain services extend Generic/Base services |
| Template Method | `GenericService` → `BaseOccurrenceService` → domain service |
| DI composition | Host `Extentions/DependencyInjection.cs`: `InitializeDefaultService`, `AddInfrastructure` |
| Result envelope | `SuccessResult<T>` |
| Exception filter | `CustomExceptionFilter` |
| Mapper | AutoMapper `Profile` classes; shared base profiles where shapes repeat |
| Auth | JWT + policy-based authorization |
| Worker/queue | Hosted consumers inheriting shared process base |

---

## 5. Generics, Base Classes, Reuse

**Prefer generics when logic repeats across entities:**

- `GenericRepository<T, TContext>`
- `GenericService<TDto, TEntity>`
- Domain-specific bases (`BaseOccurrenceService`, history/identity/assembly bases)
- Shared entity/DTO interfaces (`IBaseOccurrence`, `IBaseOccurenceDto`)
- Shared API `BaseController` for result negotiation

**Rule:** Extract duplicated CRUD/search/history logic upward into Generic.*; keep domain code for domain rules only.

---

## 6. Naming Conventions

| Area | Convention |
|------|------------|
| Projects | `Origence.{Domain}.{Layer}` |
| Namespaces | Match project/folder |
| Classes | PascalCase, noun phrases (`PartOccurrenceService`) |
| Interfaces | `I` prefix (`IPartOccurrenceRepository`) |
| Repositories | `{Entity}Repository` |
| Services | `{Feature}Service` |
| DTOs | `{Name}Dto` |
| Files | One primary type per file; file name = type name |
| Async methods | `*Async` suffix |
| Routes | kebab-case action paths |
| DB tables | snake_case |
| Enums | Centralized in `Generic.Enums` |

---

## 7. API / Controller Standards

- `[ApiController]`, route by controller
- Thin controllers: validate auth/policy → call service → return result
- Prefer inheriting `BaseController` for shared HTTP/result helpers
- Event/action endpoints return `SuccessResult<T>`
- Global `CustomExceptionFilter` for consistent error payloads
- Auth via shared authentication extensions + role/event policies
- Composition root stays thin: `Program.cs` delegates to DI extensions

---

## 8. Service Layer Standards

- Interface in `Interfaces/`, implementation in `Services/`
- Async by default (`Task` / `Task<T>`)
- Inherit Generic/Base services for CRUD-like domains
- Orchestration services compose abstractions (`IGenericContext`, validators, helpers)
- Business rules live in services (or shared helpers), not controllers
- Validators as injectable services/interfaces where request shapes are complex

---

## 9. Data Access Standards

- EF Core DbContext per domain schema
- Thin repositories inheriting generic repository
- Filtering/includes/pagination via repository contracts (`PageParam`, filters)
- Soft-delete / upsert / bulk upsert centralized in generic repository
- Avoid leaking DbContext into controllers; minimize direct DbContext use in services when a repository already exists

---

## 10. Error Handling, Logging, Validation

| Concern | Standard |
|---------|----------|
| Intentional API errors | Domain exceptions (`WebApiException` family) |
| Accumulation | `ErrorHandlers` + `ExceptionHandlerService` |
| API mapping | `CustomExceptionFilter` → `SuccessResult<WebApiError>` |
| Logging | log4net via `IOrigenceLogger` / `OrigenceLogManager` |
| Validation | Request validators + permitted-value helpers; FluentValidation where enabled |
| Config | Typed `Configurations` helper over `IConfiguration` keys |

---

## 11. DTO, Entity, Mapping Standards

- Entities: attributes / fluent config; implement shared interfaces when shapes align
- DTOs: domain Dtos project + shared Generic base DTOs
- Request models: often in `*.Models` (separate from response DTOs)
- Mapping: AutoMapper profiles in `Mapper/`; domain profiles may extend shared base profiles
- `GenericService` maps Entity ↔ DTO for standard CRUD

---

## 12. Enums & Constants

- Cross-domain enums/constants live in `Origence.Generic.Enums`
- Flat files under Enums project (no deep nesting)
- Error codes/messages as dedicated enum/static types
- Prefer shared names over copy-pasted string literals

---

## 13. DI Registration Standards

Host pattern:

```text
Program.cs
  → InitializeDefaultService(configuration)  // DbContext, auth, swagger, filters
  → AddInfrastructure(configuration)         // services, repos, workers, redis
  → Authorization policies
```

- **Scoped:** services, repositories, validators, request helpers
- **Singleton:** queue, redis multiplexer, object storage
- **Transient:** middleware clients, configuration wrappers
- Prefer extension methods over a giant inline `Program.cs`

---

## 14. Clean Code / SOLID Expectations

**Do:**

- Single responsibility per service/controller method group
- Depend on interfaces (DIP)
- Reuse generics (DRY)
- Keep controllers thin (KISS at presentation edge)
- Name types/methods for intent

**Avoid:**

- Fat controllers with business logic
- Copy-paste CRUD across domains
- Dead/commented-out base classes left in tree
- Inconsistent return envelopes
- Secrets hard-coded without config abstraction

---

## 15. Performance Practices

- Prefer DB-side filtering/pagination when `PageParam` is provided
- Use includes deliberately (avoid N+1 and over-fetch)
- Async I/O end-to-end
- Bulk upsert for batch writes
- Cache only where infrastructure already supports it intentionally

---

## 16. Formatting & Maintainability

- One class per file (primary type)
- Interfaces co-located in `Interfaces/` folders
- Meaningful method names; avoid abbreviations unless domain-standard
- Remove unused usings, dead code, and obsolete comments
- Prefer small focused methods over multi-hundred-line blocks when extracting does not change behavior

---

## Quick Checklist for RxIntel Alignment

1. Thin `Program.cs` + `Extensions/DependencyInjection.cs`
2. Controllers → service interfaces only
3. Services inherit/use GenericService where appropriate
4. Repositories inherit GenericRepository; remain thin
5. Consistent `SuccessResult` / exception filter usage
6. Shared naming: `I{Name}Service`, `{Name}Dto`, `*Async`
7. Enums/constants centralized
8. No dead BaseService / unused packages
9. Prefer BaseController where result helpers apply
10. Preserve API contracts and DB behavior while refactoring
