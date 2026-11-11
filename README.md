# Vakantiepark D'r op Uut

Digitaal platform voor Vakantiepark D'r op Uut: zoeken en boeken van verblijven, een beheersomgeving
voor medewerkers en een PWA voor gasten met parkinformatie en zwembadreserveringen.

Examenopdracht K1W3 – Graafschap College.

## Snel starten

1. Installeer de benodigdheden uit [SETUP.md](SETUP.md) (deel A1): .NET 10 SDK, Docker Desktop, Visual Studio.
2. Zorg dat Docker Desktop draait.
3. Open `Vakantiepark.slnx`, zet **Vakantiepark.AppHost** als startup project en druk op **F5**.

Het Aspire-dashboard opent en start PostgreSQL, de API en de frontend.

| Onderdeel | URL |
|-----------|-----|
| Website   | https://localhost:4200 |
| API (Swagger) | https://localhost:5001/api |

## Documentatie

- [SETUP.md](SETUP.md) – installeren, starten, migraties en database.
- [AGENTS.md](AGENTS.md) – architectuur, codeerregels en werkafspraken. Lees dit voordat je code schrijft.

## Structuur

| Project | Inhoud |
|---------|--------|
| `Vakantiepark.AppHost` | Start PostgreSQL (Docker), API en frontend |
| `Vakantiepark.Domain` | Entiteiten en interfaces |
| `Vakantiepark.Application` | Businesslogica, validatie, mapping |
| `Vakantiepark.Infrastructure` | EF Core, PostgreSQL, repositories, seeders |
| `Vakantiepark.API` | Web API |
| `Vakantiepark.Shared` | DTO's gedeeld door API en frontend |
| `Vakantiepark.UI` | Blazor WebAssembly frontend |
| `Vakantiepark.UnitTests` | xUnit + Moq tests |
