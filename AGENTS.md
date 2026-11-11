# AGENTS.md — Project rules

Read this file completely before you change anything in this repository. It describes how the
project is built and the rules every change must follow. If a rule here conflicts with a habit or
a default from a template, this file wins.

The solution is called `Vakantiepark`. Every project, namespace and folder is prefixed with it.

---

## 0. Project context

Vakantiepark D'r op Uut (exam case K1W3, Graafschap College) gets one platform with three parts that
share the same API and database:

| Part | Users | What it does |
|------|-------|--------------|
| Public website + **Zoek en boek** | Visitors | Find and book a holiday home or camping pitch |
| **Beheeromgeving** | Medewerker, Beheerder | Reservations, daily arrivals/departures, occupancy, maintenance, park settings, pool capacity |
| **PWA** | Guests (login with booking number + email) | Park information (partly offline), pool reservations |

Business rules that apply everywhere (details are in the GitHub issues US-01 … US-30):

- 200 physical units in 12 types come from `accommodaties_dr_op_uut.json`. Availability is checked per
  **physical unit**: no overlapping active booking and not out of service for the whole period.
- Arrival from 15:00, departure before 10:00, so a departure day and an arrival day may be the same.
- Price = nightly price of the season each night falls in + final cleaning (once) + tourist tax
  (persons × nights × rate). The server always recalculates; the price is stored on the booking.
- High season has a minimum number of nights. Times, tax, minimum nights, seasons and prices are
  **settings in the database**, never constants in code.
- A pool reservation always belongs to an active booking, falls within the stay, and never exceeds
  the slot's capacity, including under concurrent requests.
- Collect as little personal data as possible (name, email, phone). A guest only ever sees their own data.

UI text, validation messages and exception messages shown to users are in **Dutch**. Code, comments,
commit messages and these docs are in English.

---

## 1. Architecture

The solution uses **Clean Architecture** (also known as Onion Architecture): a layered backend with
the business core in the middle and the outside world (database, mail, HTTP) at the edges. A
**Blazor WebAssembly** frontend talks to the backend only over HTTP. The database is
**PostgreSQL**, run in Docker and started by an **Aspire AppHost** together with the API and UI.

```
Vakantiepark.slnx
├── src/
│   ├── Vakantiepark.AppHost         Aspire orchestrator: starts PostgreSQL (Docker), API and UI. The startup project.
│   ├── Vakantiepark.Domain          Entities, enums, repository and helper interfaces. No dependencies.
│   ├── Vakantiepark.Application     Business logic: services, validation rules, mapping, email templates.
│   ├── Vakantiepark.Infrastructure  EF Core, repositories, migrations, seeders, mail, hashing.
│   ├── Vakantiepark.API             ASP.NET Core Web API: controllers, auth, middleware, startup.
│   ├── Vakantiepark.Shared          Request/response DTOs shared by API and UI. No dependencies.
│   └── Vakantiepark.UI              Blazor WebAssembly frontend.
└── tests/
    └── Vakantiepark.UnitTests       xUnit + Moq tests for the Application layer.
```

### Dependency rules (never break these)

| Project        | May reference                         |
|----------------|---------------------------------------|
| Domain         | nothing                               |
| Shared         | nothing                               |
| Application    | Domain, Shared                        |
| Infrastructure | Domain                                |
| API            | Application, Infrastructure (and transitively Shared) |
| UI             | Shared only                           |
| UnitTests      | Application                           |
| AppHost        | API, UI (orchestration only, no code from them is used) |

- Domain defines **interfaces** (`IUserRepository`, `IEmailSender`, `IPassword`); Infrastructure
  **implements** them. Application only ever sees the interfaces.
- Application must not reference Infrastructure, and must not use `DbContext` or EF queries.
  Data access goes through repositories.
- The UI never references Domain, Application or Infrastructure. It only knows `Shared` DTOs and
  the HTTP API.
- Do not add a new project or a new project reference without being asked.
- The AppHost only describes **what runs** (containers, projects, connection strings). It never
  contains business logic and no other project references it.

---

## 2. Where things go

### Domain
| What                         | Location                                         |
|------------------------------|--------------------------------------------------|
| Entities                     | `Entities/{Name}.cs`                             |
| Enums                        | `Enums/{Name}.cs`                                |
| Base interfaces              | `Interfaces/IEntity.cs`                          |
| Repository interfaces        | `Interfaces/Repositories/I{Name}Repository.cs`   |
| Helper interfaces            | `Interfaces/Helpers/I{Name}.cs`                  |
| Query filters / models       | `Filters/`, `Models/`                            |
| Attributes                   | `Attributes/`                                    |

### Application
| What                                   | Location                                              |
|----------------------------------------|-------------------------------------------------------|
| Service interfaces                     | `Interfaces/I{Feature}Service.cs`                     |
| Service implementations                | `Services/{Feature}Service.cs`                        |
| Business rule validation               | `Validations/{Feature}/{Request}Validation.cs`        |
| Mapping (entity ⇄ DTO)                 | `Extensions/{Feature}Extensions.cs`                   |
| API-only DTOs (e.g. with `IFormFile`)  | `DTOs/{Feature}/`                                     |
| Email templates (embedded resources)   | `Templates/Email/{Name}.html` + `{Name}.txt`          |
| DI registration                        | `Dependencies.cs`                                     |

### Infrastructure
| What                          | Location                                            |
|-------------------------------|-----------------------------------------------------|
| DbContext                     | `Persistence/ApplicationDBContext.cs`               |
| Entity configuration          | `Persistence/Configurations/{Entity}Configuration.cs` |
| Migrations                    | `Persistence/Migrations/`                           |
| Seeders                       | `Persistence/Seeders/{Entity}Seeder.cs`             |
| EF interceptors               | `Persistence/Interceptors/`                         |
| Repositories                  | `Repositories/{Entity}Repository.cs`                |
| Mail and hashing helpers      | `Helpers/`                                          |
| Query extensions              | `Extensions/`                                       |
| DI registration               | `Dependencies.cs`                                   |

### API
| What         | Location                          |
|--------------|-----------------------------------|
| Controllers  | `Controllers/{Feature}Controller.cs` |
| Middleware   | `Middlewares/`                    |
| JSON converters | `Converters/`                  |
| DI registration | `Dependencies.cs`              |

### Shared
- `DTOs/{Feature}/{Action}{Feature}Request.cs` and `{Feature}{Kind}Response.cs`
- `DTOs/ApiResponse.cs`, `DTOs/ApiErrors.cs` — the response envelope (see §5).

### UI
| What                               | Location                                         |
|------------------------------------|--------------------------------------------------|
| Pages (routable)                   | `Pages/{Feature}/Page{Name}.razor` + `.razor.css`|
| Feature components                 | `Pages/{Feature}/{Name}Component.razor`          |
| Shared components                  | `Pages/Shared/{Name}Component.razor`             |
| Layout                             | `Layout/`                                        |
| API client interfaces              | `Application/Interfaces/I{Feature}Service.cs`    |
| API client implementations         | `Application/Services/{Feature}Service.cs`       |
| UI-only DTOs                       | `Application/DTOs/`                              |
| UI helpers                         | `Application/Extensions/`                        |
| Auth (JWT state, token handler)    | `Authentication/`                                |
| Global CSS / JS                    | `wwwroot/css/`, `wwwroot/js/`                    |
| DI registration                    | `Dependencies.cs`                                |

---

## 3. Business rules and data rules

### Entities
- Every entity implements `IEntity`:
  ```csharp
  Guid Id; DateTime CreatedAt; DateTime? UpdatedAt; DateTime? DeletedAt;
  ```
- Entities are plain classes with properties only. No business logic, no EF attributes (use the
  configuration classes).
- Order inside an entity: `Id` → (`Slug`) → data properties → timestamps → `// Navigation Keys` →
  `// Navigation Properties`.
- Strings default to `string.Empty`; collections default to `[]`; required navigations use `= null!`.

### Soft delete — always
- Nothing is hard-deleted. `DeleteAsync` sets `DeletedAt = DateTime.UtcNow`.
- **Every query must filter `DeletedAt == null`** unless the method name explicitly says otherwise,
  e.g. `GetByEmailAndActivityIdIncludingDeletedAsync`.
- Restoring a soft-deleted record (setting `DeletedAt = null`) is preferred over creating a duplicate.

### Timestamps
- Always `DateTime.UtcNow`. Never `DateTime.Now`.
- `CreatedAt` / `UpdatedAt` / `DeletedAt` are set by the generic `Repository<T>`, not by services.
- The API serialises all dates as UTC (`UtcDateTimeJsonConverter`). The UI converts to local time
  for display.

### Identifiers and slugs
- Primary keys are `Guid`.
- Entities that appear in public URLs get a unique, lowercase `Slug` (accommodation types already
  have one in the source JSON, e.g. `eik-luxe-6`). Public pages use the slug, never the Guid.
- Booking numbers shown to guests are random and not sequential, so they can't be guessed.

### Validation — two levels
1. **Shape validation** (required, length, email format) lives as DataAnnotations on the `Shared`
   request DTOs. ASP.NET rejects invalid models automatically with an `ApiResponse.Error` listing
   per-field errors.
2. **Business rules** (deadlines, capacity, permissions, uniqueness, state) live in
   `Application/Validations/{Feature}/{Request}Validation.cs` as a static extension method:
   ```csharp
   public static void Validate(this SignUpRequest request, Activity? activity, Guid userId, int totalSignUps)
   {
       if (activity is null)
           throw new ArgumentException("Er is geen activiteit gevonden.");

       if (totalSignUps >= activity.MaxParticipants)
           throw new ArgumentException("Deze activiteit heeft al het maximum aantal deelnemers.");
   }
   ```
   The service loads what the rules need, then calls `request.Validate(...)` **before** changing
   anything.

### Exceptions as the error contract
| Situation                              | Throw                        | Controller returns |
|----------------------------------------|------------------------------|--------------------|
| Business rule broken / bad input       | `ArgumentException(message)` | `400 BadRequest`   |
| Requested item does not exist          | `KeyNotFoundException(message)` | `404 NotFound`  |
| Programming / config error             | `InvalidOperationException`  | falls through to `/Error` → `500` |

- The exception **message is shown to the end user**, so it must be a complete, friendly sentence in
  the app's UI language (Dutch). Do not put stack details or IDs in it.
- Never catch `Exception` in services or controllers. Unexpected errors go to the global handler.

### Authentication and authorisation
- JWT bearer tokens. The user id is read from `ClaimTypes.NameIdentifier`, the email from
  `ClaimTypes.Email`, the name from `ClaimTypes.Name`.
- Role checks use `[Authorize(Roles = "Medewerker,Beheerder")]` or `[Authorize(Roles = "Beheerder")]` on controller actions.
- Never trust ids, emails or roles sent in the request body when they are available from the token;
  overwrite them from the claims in the controller.
- An anonymous caller is represented in services by `Guid.Empty` as `userId`.
- Deactivated users are rejected on every request in `OnTokenValidated`.
- Passwords are hashed through `IPassword` (BCrypt). Never store or log plain passwords or tokens.
- A global rate limiter applies (fixed window per user / IP). Keep it on.

---

## 4. Code style

The goal is code that reads like it was written by one careful person. Match the existing files.

### General C#
- .NET 10, `Nullable` and `ImplicitUsings` enabled in every project.
- **Block-scoped namespaces** (`namespace X { ... }`), matching the folder path.
- **Explicit types** instead of `var` (`User? user = ...`, `List<string> items = [];`).
  `var` is only tolerated in the generated `Program.cs` builder lines.
- **Constructor injection** with a classic constructor (no primary constructors).
- Private readonly dependency fields are **PascalCase without underscore** in production code:
  ```csharp
  private readonly IUserRepository UserRepository;
  ```
  (Test classes use `_camelCase`, see §7.)
- `const` values at the top of the class, PascalCase (`PasswordSetupValidMinutes`).
- Every async method ends in `Async` and returns `Task`/`Task<T>`. No `async void`. No `.Result` / `.Wait()`.
- Use `is null` / `is not null`, never `== null` on references (EF lambdas excepted, where
  `x.DeletedAt == null` is required for translation).
- Use collection expressions: `[]`, `[.. items]`.
- Use expression-bodied members (`=>`) for one-line methods, especially repository queries and UI
  API wrappers.
- Guard clauses: single-line `if` + statement without braces, followed by a blank line.
  ```csharp
  if (signUp is null)
      throw new KeyNotFoundException("Er is geen inschrijving gevonden.");
  ```
- Early `return` over nested `if/else`.
- Multi-line LINQ: one operator per line, indented under the source.
- Names say what something is. No abbreviations (`activity`, not `act`), no Hungarian notation.

### Comments
- Comment **why**, never **what**. Most methods need no comment.
- Use `/// <summary>` only on public contracts whose behaviour is not obvious from the name
  (for example a `DesignTimeDbContextFactory` that explains why it exists).
- No commented-out code, no `TODO` without an issue reference, no banner comments.

### What "not slop" means here
- No speculative abstractions, base classes or generics for a single use.
- No dead code, unused usings, unused parameters, or empty catch blocks.
- No duplicated logic: if two services need the same rule, it goes in a validation or extension.
- No magic numbers: name them as `const` or put them in configuration.
- No new NuGet package without a clear reason stated in the PR.
- Keep methods short; a service method reads as: load → validate → act → return.

---

## 5. API conventions

### Controllers are thin
A controller action only: reads claims, calls **one** service method, maps exceptions to status
codes, wraps the result. No business logic, no repository access.

```csharp
[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService UserService;

    public UserController(IUserService userService)
    {
        UserService = userService;
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Beheerder")]
    public async Task<IActionResult> GetByIdAsync(Guid id)
    {
        try
        {
            UserResponse user = await UserService.GetByIdAsync(id);
            return Ok(ApiResponse<UserResponse>.Ok(user));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(ApiResponse<UserResponse>.Fail(exception.Message));
        }
    }
}
```

### Response envelope — every endpoint
Every response body is an `ApiResponse<T>`:
```json
{ "value": ..., "succeeded": true, "message": "optional text", "errors": [] }
```
- Success: `ApiResponse<T>.Ok(value, message)`; use `ApiResponse<object>.Ok()` when there is no value.
- Handled failure: `ApiResponse<T>.Fail(message)`.
- Model-state failure: `ApiResponse<T>.Error(List<ApiErrors>)` (generated automatically).
- 401/403/429/500 responses are also wrapped (configured in `API/Dependencies.cs`). Keep it that way.

### Routes
- `[Route("[controller]")]` — the route is the controller name, **PascalCase**.
- Sub-routes are PascalCase too: `"SignOut/Confirmation/{token}"`. The `LogEndpointMiddleware`
  warns in development when a segment's casing doesn't match the controller.
- Verbs: `GET` read, `POST` create/action, `PUT` update, `DELETE` remove.
  `QUERY` is used for reads that need a body (UI calls it through `IHttpService.QueryAsync`).

---

## 6. Persistence conventions (EF Core + PostgreSQL)

- Provider: `Npgsql.EntityFrameworkCore.PostgreSQL`, registered in `Infrastructure/Dependencies.cs`
  with `UseNpgsql(connectionString)`. Infrastructure receives the connection string as a parameter;
  it does not know about Aspire or Docker.
- Tables and columns use **snake_case** (`UseSnakeCaseNamingConvention()`), the PostgreSQL norm.
  C# names stay PascalCase; never hand-write quoted PascalCase identifiers in SQL.
- `DateTime` values written to the database **must be `DateTimeKind.Utc`** (Npgsql maps them to
  `timestamp with time zone` and throws on `Local`). This is another reason to only use
  `DateTime.UtcNow`, including in seeders and tests.
- `DbSet<T>` properties are expression-bodied: `public DbSet<User> Users => Set<User>();`
- Configuration is a static extension per entity, called from `OnModelCreating`:
  ```csharp
  public static class SignUpConfiguration
  {
      public static void Configure(this EntityTypeBuilder<SignUp> builder) { ... }
  }
  // OnModelCreating:
  builder.Entity<SignUp>(options => options.Configure());
  ```
- Configure relationships explicitly with `HasOne/WithMany/HasForeignKey/OnDelete`. Prefer
  `DeleteBehavior.Restrict` or `SetNull`; never rely on cascade for soft-deleted data.
- Add unique indexes for business uniqueness (e.g. one sign-up per user per activity).
- Repositories:
  - `Repository<TEntity>` (generic) gives `GetAll`, `GetById`, `Add`, `Update`, `Delete`.
  - A specific repository **inherits** it and its interface **extends** `IRepository<T>`.
  - Specific query methods are named for exactly what they return:
    `GetUpcomingByUserIdAsync`, `CountByActivityIdAsync`, `HasSignedUpAsync`.
  - Bulk changes use `ExecuteUpdateAsync`.
  - Only `Include` what the caller needs.
- Migrations live in `Infrastructure/Persistence/Migrations`, the history table is named
  `migrations`, and the API applies them on startup (`Database.Migrate()`). A
  `DesignTimeDbContextFactory` in Infrastructure lets `dotnet ef` run without Docker.
- Never assume the database exists or has data: it is a fresh container on a new PC. Startup
  migration + idempotent seeders must always produce a working app.
- Migration names are PascalCase verbs describing the change: `AddedIsActiveToUser`.
- **Never edit a migration that is already on `main`.** Add a new one.
- Seed data goes through `UseSeeding` and `{Entity}Seeder` extensions; seeders must be idempotent
  (check before inserting).

---

## 7. Tests

- Framework: **xUnit + Moq**. Tests target the **Application** layer (services, validations,
  extensions). Repositories and helpers are mocked.
- One test class per unit: `{ServiceName}Tests.cs`.
- Test naming: `Method_Scenario_ExpectedResult`
  - `SignUpAsync_ActivityFull_ThrowsArgumentException`
  - `SignOutAsync_Employee_DeletesSignUpAndNotifiesWaitlist`
- Fields in test classes use `_camelCase`; the class under test is `_sut`.
- Arrange/Act/Assert separated by blank lines (no `// Arrange` comments needed).
- Test data comes from `TestData.cs` builders that take an optional `Action<T> configure`, so each
  test only sets the values it is about:
  ```csharp
  SignUp signUp = TestData.CreateSignUp(_activity.Id, x => x.WaitlistPosition = 1);
  ```
- Every business rule in `Validations/` gets at least one test for the failing case.
- Every bug fix comes with a test that would have caught it.
- Use `Verify(..., Times.Once/Never)` to prove side effects (saved, mailed, not saved).

---

## 8. Frontend conventions (Blazor WebAssembly)

- Pages call **UI services** (`Application/Services/{Feature}Service.cs`), never `HttpClient`
  directly. UI services are one-line wrappers over `IHttpService`:
  ```csharp
  public async Task<ApiResponse<object>> SignUpAsync(SignUpRequest request)
      => await HttpService.PostAsync<object, SignUpRequest>("/SignUp", request);
  ```
- `IHttpService` never throws; it always returns an `ApiResponse<T>`. Pages check
  `response.Succeeded` and show `response.Message` via the notifications component.
- Every page handles three states: loading (spinner), failed (message), empty (message).
- Page routes are lowercase and in the UI language (`/zoek-en-boek`, `/beheer/reserveringen`).
- Protected pages use `@attribute [Authorize]` / `[Authorize(Roles = "Beheerder")]`. The API still
  enforces auth; UI checks are for UX only.
- Styles: component-scoped `.razor.css` next to the component. Global styles only in `wwwroot/css`.
  Use the existing CSS variables/theme; don't hardcode new colours.
- `@code` block order: fields → computed properties → lifecycle methods → event handlers → helpers.
- Culture is set once in `Program.cs` (e.g. `nl-NL`).

---

## 9. Dependency injection

Each layer exposes one public extension in its `Dependencies.cs` and splits it into private
`Register*` methods:

```csharp
public static IServiceCollection RegisterApplication(this IServiceCollection services)
{
    services.RegisterServices();
    return services;
}
```

- `Program.cs` only calls `RegisterAPI`, `RegisterApplication`, `RegisterInfrastructure(...)`.
- Services and repositories are `Scoped`. Use `Singleton` only for stateless or deliberately
  in-memory state (and resolve scoped dependencies through `IServiceScopeFactory`).
- **When you add a service or repository, register it** in the matching `Dependencies.cs`.

---

## 10. Configuration and secrets

- Required configuration is read with a fail-fast check:
  ```csharp
  builder.Configuration.GetConnectionString("DefaultConnection")
      ?? throw new InvalidOperationException("The connectionString was not found!");
  ```
- **Secrets never go in `appsettings.json`** (JWT key, SMTP password, connection strings with
  passwords). Use `dotnet user-secrets` locally and environment variables / GitHub secrets elsewhere.
- The database connection string is **not configured by hand**. The AppHost generates the Postgres
  password and injects `ConnectionStrings:vakantiepark` into the API. Do not add a connection
  string with a password to any `appsettings*.json`.
- The project must start on a clean PC with only the prerequisites in SETUP.md installed. Any change
  that adds a manual setup step (a local install, a hand-made secret, a script to run first) must be
  avoided, or automated in the AppHost, or added to SETUP.md in the same PR.
  `appsettings.json` holds only non-secret defaults and empty placeholders.
- Frontend config (`UI/wwwroot/appsettings.json`) is public by definition — only the API base URL.

---

## 11. Git and CI

- **Never commit or push directly to `main`.** It is protected on GitHub and a direct push is rejected.
- Every change, however small, goes through this flow:
  1. Update `main` locally (`git checkout main` → `git pull`).
  2. Create a branch from `main`: `git checkout -b <branch-name>`.
  3. Commit on that branch and push it (`git push -u origin <branch-name>`).
  4. Open a pull request into `main` and link the issue it finishes (`Closes #6`).
  5. **One other team member must approve** the pull request. You cannot approve your own.
  6. Merge only when the approval is there **and** CI is green. Delete the branch after merging.
- New commits pushed to a pull request after an approval need a fresh look from the reviewer.
- Resolve merge conflicts on your own branch (merge or rebase `main` into it), never on `main`.
- Branch names: kebab-case, describing the change: `zoeken-beschikbaarheid`, `fix-prijs-seizoensgrens`.
  One branch per user story or fix; keep pull requests small enough to review in one sitting.
- Commit messages: imperative, short subject, optionally a conventional prefix:
  `feat(boeken): add booking confirmation email`, `fix(prijs): use season of each night`.
- CI (`.github/workflows/ci-build-test.yml`) runs on every PR to `main`:
  restore → build (Release) → test. **A PR is only mergeable when CI is green and one team member
  has approved it.**
- Agents: never push to `main`, never merge a pull request and never approve one. Work on a branch,
  push it, and leave the pull request and its review to the team.
- CI does not need Docker: unit tests never touch a real database.
- Before you consider a change done, run locally:
  ```bash
  dotnet build --configuration Release
  dotnet test
  ```
  Both must pass with no new warnings.
- Do not commit `bin/`, `obj/`, `.vs/`, `*.user` files or secrets.

---

## 12. Checklist for adding a feature

1. **Domain** — entity (implements `IEntity`), enum(s), `I{Entity}Repository`.
2. **Infrastructure** — `{Entity}Configuration`, `DbSet`, `{Entity}Repository`, register in
   `Dependencies.cs`, add a migration (`dotnet ef migrations add ...`, see SETUP.md §9).
3. **Shared** — request/response DTOs with DataAnnotations.
4. **Application** — `I{Feature}Service` + `{Feature}Service`, `{Request}Validation`,
   `{Feature}Extensions` (mapping), register in `Dependencies.cs`.
5. **Tests** — `{Feature}ServiceTests` covering the happy path and each business rule.
6. **API** — thin `{Feature}Controller`, `ApiResponse<T>` everywhere, correct `[Authorize]`.
7. **UI** — `I{Feature}Service` + `{Feature}Service` wrapper, pages/components, register in
   `Dependencies.cs`.
8. Build + test locally, open a PR, wait for green CI.
