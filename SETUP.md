# SETUP.md — Installing, running and working on the project

Vakantiepark D'r op Uut is a **Clean Architecture** solution on **.NET 10**: an ASP.NET Core Web API,
a **Blazor WebAssembly** frontend and **PostgreSQL** in **Docker**, all started by one
**Aspire AppHost** project.

On any PC with the prerequisites installed you clone the repo, set `Vakantiepark.AppHost` as startup
project and press **Run**. Aspire starts the PostgreSQL container, creates the database, starts the
API (which applies migrations and seed data) and starts the frontend. No database install, no
connection strings, no secrets to set by hand.

Read [AGENTS.md](AGENTS.md) before writing code: it contains the architecture and code rules.

---

## 1. Prerequisites (once per PC)

Install everything from a terminal with `winget` (built into Windows 10/11):

```powershell
winget install -e --id Microsoft.DotNet.SDK.10
winget install -e --id Docker.DockerDesktop
winget install -e --id Git.Git
winget install -e --id Microsoft.VisualStudio.Community   # or JetBrains.Rider
```

Then, once:

```powershell
dotnet dev-certs https --trust      # trust the local HTTPS certificate
```

| Tool | Why |
|------|-----|
| .NET 10 SDK | builds and runs everything (`global.json` pins the 10.0 SDK line) |
| Docker Desktop | runs the PostgreSQL container; Aspire starts it for you |
| Git | version control |
| Visual Studio 2026 with the *ASP.NET and web development* workload | IDE |

Docker Desktop on Windows uses WSL 2; the installer sets it up and may ask for a restart.
Docker must be **running** (whale icon in the tray) before you press Run.

> **Older / low-RAM laptops:** in Docker Desktop → *Settings → Resources* lower the memory limit
> (2–3 GB is enough). Alternatively install **Podman Desktop**
> (`winget install -e --id RedHat.Podman-Desktop`) instead of Docker and set the environment variable
> `ASPIRE_CONTAINER_RUNTIME=podman`. Aspire supports both.

---

## 2. Getting the code

```powershell
git clone https://github.com/GC-Group-5/Examenopdracht-Vakantiepark.git
cd Examenopdracht-Vakantiepark
```

---

## 3. Running the project

**Visual Studio:** open `Vakantiepark.slnx`, right-click `Vakantiepark.AppHost` →
*Set as Startup Project*, press **F5**.

**CLI:**
```powershell
dotnet run --project Vakantiepark.AppHost
```

The **Aspire dashboard** opens in your browser. It shows every part of the app with its logs and URLs:

| Resource | URL | |
|----------|-----|---|
| ui       | https://localhost:4200 | the website |
| api      | https://localhost:5001/api | Swagger |
| pgadmin  | link in the dashboard | browse the database |
| postgres | container | managed by Aspire |

The first run downloads the PostgreSQL and pgAdmin images and takes a few minutes. After that it
starts in seconds and the database keeps its data between runs.

### Resetting the database

The data lives in a Docker volume. To start completely fresh: stop the app, open Docker Desktop →
*Volumes*, delete the volume whose name contains `postgres-data`, and run again. Migrations and
seeders rebuild everything.

### If something doesn't start

| Symptom | Fix |
|---------|-----|
| Dashboard says the container runtime is unhealthy | Start Docker Desktop and wait until it says *running* |
| Browser warns about the certificate | `dotnet dev-certs https --clean`, then `dotnet dev-certs https --trust` |
| Port 4200 or 5001 is already in use | Stop the other app, or change the port in `launchSettings.json` **and** in the settings in §4 |
| API logs "connection string 'vakantiepark' was not found" | You started the API directly. Start `Vakantiepark.AppHost` instead |
| Not sure what's wrong | Install the Aspire CLI (`dotnet tool install -g Aspire.Cli`) and run `aspire doctor` |

---

## 4. How the pieces are connected

### AppHost — `Vakantiepark.AppHost/AppHost.cs`

```csharp
var postgres = builder.AddPostgres("postgres")
    .WithImageTag("17")                              // everybody runs the same major version
    .WithDataVolume()                                // data survives restarts
    .WithLifetime(ContainerLifetime.Persistent)      // container keeps running: fast restarts
    .WithPgAdmin();                                  // database GUI, linked from the dashboard

var database = postgres.AddDatabase("vakantiepark");

var api = builder.AddProject<Projects.Vakantiepark_API>("api")
    .WithReference(database)                         // injects ConnectionStrings:vakantiepark
    .WaitFor(database);

builder.AddProject<Projects.Vakantiepark_UI>("ui")
    .WaitFor(api);
```

- Aspire generates the Postgres password and stores it in the AppHost's user secrets on your PC.
  Nobody types or commits it.
- New secrets (the JWT signing key in US-10, SMTP settings, …) are added the same way: an Aspire
  parameter with a generated value, passed to the API with `WithEnvironment`. Never in `appsettings.json`.
- A local mailbox (Mailpit) is added to the AppHost in US-09, so development never sends real email.

### Ports and settings

| Where | Setting | Value |
|-------|---------|-------|
| `Vakantiepark.API/Properties/launchSettings.json` | API URL | `https://localhost:5001` |
| `Vakantiepark.UI/Properties/launchSettings.json` | Frontend URL | `https://localhost:4200` |
| `Vakantiepark.API/appsettings.json` | `Frontend:BaseUrl` (CORS) | `https://localhost:4200` |
| `Vakantiepark.UI/wwwroot/appsettings.json` | `ApiBaseUrl` | `https://localhost:5001` |

These four must stay in line. CORS only allows `Frontend:BaseUrl`.

### Database registration

`Infrastructure/Persistence/DatabaseOptions.cs` holds the provider settings in one place
(Npgsql, snake_case names, migrations table `migrations`). It is used by the runtime registration in
`Infrastructure/Dependencies.cs` **and** by `DesignTimeDbContextFactory`, so the EF tools always
match the running app.

---

## 5. Database work

### Migrations

```powershell
# create (from the repo root; Docker does not need to be running)
dotnet ef migrations add AddedAccommodations `
  --project Vakantiepark.Infrastructure `
  --startup-project Vakantiepark.API `
  --output-dir Persistence/Migrations

# remove the last migration (only if it has NOT been pushed)
dotnet ef migrations remove --project Vakantiepark.Infrastructure --startup-project Vakantiepark.API
```

Install the EF tool once per PC: `dotnet tool install --global dotnet-ef`.

You never run `database update`: the API applies pending migrations every time it starts.
Name migrations after what changed (`AddedAccommodations`, `AddedBookingNumberToBooking`).
**Never edit a migration that is already on `main`**, add a new one.

### PostgreSQL things to know

- Tables and columns are **snake_case** (`bookings.arrival_date`). Your C# stays PascalCase.
- `DateTime` must be **UTC** when saved (`DateTime.UtcNow`). Npgsql throws on `DateTime.Now`.
- Text comparisons are **case-sensitive**. Store emails lowercased, or use `EF.Functions.ILike`.
- Guid primary keys map to the native `uuid` type.

### Looking at the data

Open **pgadmin** from the Aspire dashboard; it is already connected. Or use any client (DBeaver,
Rider / Visual Studio database tools) with the connection string shown on the `vakantiepark`
resource in the dashboard.

---

## 6. Working together

### Branches and pull requests

`main` is protected on GitHub: **nobody can push to it directly**. Every change goes through a
branch and a pull request that **one other team member approves** before it can be merged.

```powershell
git checkout main
git pull
git checkout -b zoeken-beschikbaarheid      # kebab-case, describes the change
# ... work, commit ...
git push -u origin zoeken-beschikbaarheid
```

Open a pull request into `main` on GitHub and mention the issue it closes (`Closes #6`), so the
board moves it to Done when it's merged. Then:

1. Ask a teammate to review. They approve it, or request changes.
2. Merge when it's **approved** and **CI is green**. GitHub blocks the merge button until both are true.
3. Delete the branch after merging (GitHub offers a button for this).

You can't approve your own pull request. If you push new commits after an approval, ask for another look.

### CI

`.github/workflows/ci-build-test.yml` runs on every pull request to `main` and on every push to `main`:
restore → build (Release) → unit tests. A pull request can only be merged when CI is green.
CI does not need Docker: unit tests never touch a real database.

Before you push, run locally:

```powershell
dotnet build Vakantiepark.slnx --configuration Release
dotnet test Vakantiepark.slnx
```

### Scrum board

GitHub Projects: **GC-Group-5 Planning**. Every user story is an issue (`US-01` … `US-30`) with
Priority, Estimate, Size and Sprint filled in. Keep the status of your own items up to date.

---

## 7. First-run checklist

- [ ] `dotnet build Vakantiepark.slnx` succeeds.
- [ ] `dotnet test Vakantiepark.slnx` passes.
- [ ] Docker Desktop is running.
- [ ] Running `Vakantiepark.AppHost` opens the Aspire dashboard with `postgres`, `vakantiepark`,
      `api`, `ui` and `pgadmin` all healthy.
- [ ] https://localhost:5001/api shows Swagger.
- [ ] https://localhost:4200 shows the homepage.
