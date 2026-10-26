# RxIntel Refactoring Log

Incremental alignment of **RxIntel Backend** to **OrigencePart** standards.  
Business behavior and API contracts preserved unless noted.

---

## Module 1 — Host DI composition root

### Files
- `Rxintel.AI/Extensions/DependencyInjection.cs` *(new)*
- `Rxintel.AI/Program.cs`

### Reason
OrigencePart keeps `Program.cs` thin and registers services via `InitializeDefaultService` / `AddInfrastructure` extension methods.

### Before → After
- **Before:** ~160-line `Program.cs` with all DI inline  
- **After:** `Program.cs` calls two extension methods; registrations live in `DependencyInjection.cs`

### Benefits
Discoverable composition root; easier to review new service registrations; matches OrigencePart host pattern.

### Risks
Low — registration order and lifetimes unchanged. AutoMapper consolidated to a single `AddAutoMapper(params Type[])` call (equivalent).

### Suggested commit
```
refactor(host): extract DI into DependencyInjection extensions like OrigencePart
```

---

## Module 2 — Dead code, packages, naming hygiene

### Files
- Deleted: `Rxintel.Generic.Services/Services/BaseService.cs`, `Interfaces/IBaseService.cs`
- `Rxintel.Generic.Api/Rxintel.Generic.Api.csproj` (removed Nancy)
- Renamed: `WholeselarDto.cs` → `WholesalerDto.cs`, `IWholesalerREpository.cs` → `IWholesalerRepository.cs`, mapping/controller file casing, `Strength.cs` → `UnitStrength.cs`
- Enum rename: `UnitStrengh` → `UnitStrength` (+ validator/migration references)
- Docs cleanup: Spire/SMS references in Logger, AuthExtensions, BaseController, SuccessResult

### Reason
OrigencePart does not ship dead base classes or unused packages; file names match type names.

### Benefits
Less noise for developers; correct spelling; package surface smaller.

### Risks
- `UnitStrength` rename is compile-time only (DB stores description labels as strings) — low risk.
- File renames only where type names were already correct — no route changes.

### Suggested commit
```
chore: remove dead BaseService/Nancy and fix naming typos (UnitStrength, files)
```

---

## Module 3 — Controllers + layering

### Files
- All `Rxintel.AI/Controllers/*Controller.cs` now inherit `BaseController` (parameterless ctor; no response-shape change)
- `WholesalerService.cs`, `PharmacySoftwareService.cs` — stop injecting `RxintelDbContext`; use repositories for delete-guard queries
- Minor: Login/State constructor parameter casing

### Reason
OrigencePart controllers inherit shared `BaseController`. Services depend on repository abstractions, not EF contexts, for simple queries.

### Before → After
- Controllers: `ControllerBase` → `BaseController` (structural)
- Wholesaler/PharmacySoftware delete checks: `DbContext.AnyAsync` → repository `GetAllAsync` + `.Any()`

### Benefits
Consistent presentation base; DIP for delete guards; easier to mock.

### Risks
- Login endpoints **intentionally keep** local `catch (WebApiException)` so clients still receive a plain string body (filter would wrap `SuccessResult<WebApiError>` — API contract change avoided).
- `PharmacyService` still uses `RxintelDbContext` for multi-entity registration transactions (next module).
- Delete-guard now materializes matching rows instead of `AnyAsync`; behavior identical for conflict detection; minor perf difference only when many links exist (rare for a delete guard).

### Suggested commit
```
refactor: inherit BaseController; remove DbContext from Wholesaler/PharmacySoftware services
```

---

## Module 4 — FluentValidation version alignment

### Files
- `Rxintel.Generic.Services/Rxintel.Generic.Services.csproj`

### Reason
Common/Models used FluentValidation **11.11.0**; Services used **12.1.1**, risking runtime/API mismatches. OrigencePart keeps package versions consistent across layers.

### Before → After
Services FluentValidation `12.1.1` → `11.11.0` (matches Common/Models and `FluentValidation.AspNetCore` 11.3.1).

### Benefits
Single FluentValidation major version across the solution.

### Risks
Low — validators already written against 11.x APIs used by the host.

### Suggested commit
```
chore: align FluentValidation to 11.11.0 across all projects
```

---

## Module 5 — Unit of Work; remove DbContext from PharmacyService

### Files
- `Rxintel.Generic.Repositories/Interfaces/IUnitOfWork.cs` *(new)*
- `Rxintel.Generic.Repositories/Repositories/UnitOfWork.cs` *(new)*
- `Rxintel.Generic.Repositories/Interfaces/IGenericRepository.cs` — `AddAsync` (stage without SaveChanges)
- `Rxintel.Generic.Repositories/Repositories/GenericRepository.cs` — `AddAsync`
- `Rxintel.Generic.Services/Services/PharmacyService.cs` — registration uses UoW + repos
- `Rxintel.AI/Extensions/DependencyInjection.cs` — register `IUnitOfWork`

### Reason
OrigencePart services depend on repository/UoW abstractions, not EF `DbContext` DbSets.

### Before → After
- **Before:** `PharmacyService` injected `RxintelDbContext` for `BeginTransaction` + direct `Pharmacies`/`PharmacyUsers` adds  
- **After:** `IUnitOfWork` + repository `AddAsync` + single `SaveChangesAsync`

### Benefits
DIP restored; registration transaction behavior preserved.

### Risks
Low — same scoped DbContext; same commit/rollback/document cleanup order.

### Suggested commit
```
refactor: add IUnitOfWork and remove DbContext from PharmacyService registration
```

---

## Module 6 — Split fat services (Pharmacy registration + CSV helpers)

### Files
- `IPharmacyRegistrationService` / `PharmacyRegistrationService` *(new)*
- `PharmacyService` delegates `RegisterPharmacy`
- Helpers: `DtoColumnHelper`, `ImportRowValidationHelper`, `BillingCsvHelper`, `PurchaseCsvHelper`
- Slimmed `BillingService` / `PurchaseService`

### Reason
OrigencePart keeps orchestration and shared CSV/validation helpers out of god services.

### Benefits
Pharmacy registration isolatable; Billing/Purchase DRY for columns/validation/BIN resolution.

### Risks
Low — controller → `IPharmacyService` unchanged; CSV envelopes unchanged.

### Suggested commit
```
refactor: extract PharmacyRegistrationService and Billing/Purchase CSV helpers
```

---

## Module 7 — Safer DB-side paging (no-search paths only)

### Files
- `GenericRepository.GetAllAsync` — Skip/Take when pagination requested and SearchText empty
- `PharmacyService`, `PharmacyUserService`, `PharmacySoftwareService`, `BillingService`, `PurchaseService`, `PharmacyWholesalerService` GetAll paths

### Reason
In-memory FilterBySearch uses NormalizeForSearch (not SQL-equivalent). DB paging only when there is no search text.

### Before → After
- **No search + pagination:** count via `GetFilteredCountAsync`, page via repository Skip/Take  
- **With search:** unchanged load → FilterBySearch → `ToPagedList`

### Benefits
Less memory/IO on large lists when clients paginate without search.

### Risks
Medium for `PharmacyWholesalerService` — previously ignored Skip/Take when page params were sent; now honors them (aligned with other list endpoints).

### Suggested commit
```
perf: apply DB paging when pagination requested without search text
```

---

## Module 8 — Configurations facade (Jwt / Email / document size)

### Files
- `Rxintel.Generic.Common/Implementations/Helpers/Configurations.cs` *(new)*
- `TokenHelper`, `TokenParser`, `EmailService`, `AuthExtensions`, `LoginService`, `PharmacyService`, `PharmacyRegistrationService`
- DI: `AddTransient<Configurations>()`

### Reason
OrigencePart uses a typed configuration facade with bounded parsers over existing keys.

### Benefits
Central Jwt/Email/doc-size accessors; `PharmacyDocumentStorage:MaxFileSizeBytes` now actually applied.

### Risks
Low — same keys; Jwt expiration clamped 1–1440 minutes (default 60).

### Note
Secrets should live in User Secrets / environment variables; committed `appsettings.json` values were not rewritten in this pass.

### Suggested commit
```
refactor: add Configurations facade for Jwt, Email, and document size settings
```

---

## Module 9 — Internal error-handling discipline

### Files
- `NDCGroupService.GetMappingsByParentId` — `ArgumentException` → `WebApiException` (BadRequest) with same `CustomMessage`

### Reason
Uncaught exceptions should map through `CustomExceptionFilter` consistently.

### Do not change
- `LoginController` / `PharmacyController` local `catch (WebApiException)` plain-string bodies (API contract preserved).

### Suggested commit
```
refactor: use WebApiException in NDCGroupService for invalid parent id
```

---

## Module 10 — Validation gaps + logging hygiene

### Files
- Validators: `NDCGroupValidator`, `BillingFieldMappingValidator`, `WholesalerFieldMappingValidator`
- Admin FluentValidation skipped — would change login empty-credential / ConfirmPassword behavior

### Logging
- No checked-in `log4net.config` (listed in `.gitignore`; host does not call XmlConfigurator). Left as-is; services continue preferring `ILogger<T>` where already used.

### Suggested commit
```
feat: add FluentValidation for NDCGroup and field-mapping DTOs
```

---

## Completed modules (5–10)

| # | Focus | Status |
|---|--------|--------|
| 5 | UoW + remove PharmacyService DbContext | Done |
| 6 | Pharmacy registration + CSV helpers | Done |
| 7 | DB paging (no-search) | Done |
| 8 | Configurations facade | Done |
| 9 | Error discipline (touched code) | Done |
| 10 | Validation + logging hygiene | Done |
