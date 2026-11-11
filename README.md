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

## Testaccounts

In de ontwikkelomgeving maakt de API bij de eerste start automatisch deze accounts aan
(alleen in Development, nooit in productie):

| Rol | E-mailadres | Wachtwoord |
|-----|-------------|------------|
| Medewerker | `medewerker@dropuut.example` | `Medewerker123!` |
| Beheerder | `beheerder@dropuut.example` | `Beheerder123!` |

Inloggen als medewerker: https://localhost:4200/beheer/inloggen.
Een **gast** heeft geen account, maar logt in met het boekingsnummer (bijv. `DOU-7KQ3M9XP`) en het
e-mailadres van een boeking via `POST /Auth/Guest`.

## Werkafspraken

- **Nooit direct op `main` pushen.** `main` is beveiligd; alles gaat via een eigen branch.
- Per userstory of fix maak je een branch vanaf `main`, bijvoorbeeld `zoeken-beschikbaarheid`.
- Je opent een pull request naar `main` met de issue erin (`Closes #6`).
- **Eén ander teamlid moet de pull request goedkeuren**, en de CI-build moet groen zijn, voordat hij gemerged kan worden.

De volledige afspraken staan in [AGENTS.md](AGENTS.md) (hoofdstuk 11) en [SETUP.md](SETUP.md) (hoofdstuk 6).

## Huisstijl

De website is gebouwd op de aangeleverde **Bootstrap-template** (`dropuut_website_bootstrap`). We kozen
Bootstrap boven de Tailwind- en de losse CSS-versie omdat:

- Bootstrap 5 als één CSS-bestand werkt zonder build-stap; de aangeleverde Tailwind-CSS is vooraf
  gegenereerd, dus nieuwe classes voor onze eigen pagina's zouden een Node/Tailwind-build vereisen;
- het grid, de formulieren en de componenten van Bootstrap direct bruikbaar zijn voor de zoek- en
  boekmodule en de beheeromgeving;
- het hele team al met Bootstrap heeft gewerkt.

| Bestand | Inhoud |
|---------|--------|
| `Vakantiepark.UI/wwwroot/lib/bootstrap/bootstrap.min.css` | Bootstrap 5.3.6 (lokaal, geen CDN) |
| `Vakantiepark.UI/wwwroot/css/theme.css` | Huisstijl uit de template, met de logokleuren als CSS-variabelen (`--du-green`, `--du-leaf`, `--du-aqua`, …) |
| `Vakantiepark.UI/wwwroot/css/app.css` | Alleen Blazor-laadscherm en foutbalk |

Gebruik altijd de variabelen uit `theme.css` in plaats van losse kleurcodes.

### Afbeeldingen

Alle accommodatiefoto's staan als WebP in `Vakantiepark.UI/wwwroot/img/accommodaties/{slug}/`,
waarbij `{slug}` het type uit de JSON is (`eik-luxe-6`, `tentplaats-comfort`, …):

| Bestand | Inhoud |
|---------|--------|
| `1.webp` | Hoofdfoto (buitenkant), max. 1600 px breed |
| `2.webp`, `3.webp` | Extra foto's, max. 1200 px breed (kampeerplaatsen hebben alleen `2.webp`) |
| `plattegrond.webp` | Plattegrond (alleen vakantiewoningen) |

Het logo staat in `wwwroot/img/logo.webp`, het favicon en app-icoon in `wwwroot/favicon.png` en `wwwroot/icon-192.png`.

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
