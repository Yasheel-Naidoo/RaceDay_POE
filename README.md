# RaceDay – Event Management Platform

RaceDay is a centralized, full-stack event management platform for South African road running, walking and cycling events. It replaces manual entries with an integrated operational schema, automatic bib allocation, quota enforcement and role-based access control.

The repository contains **Part 2 (RESTful Web API)**, **Part 3 (ASP.NET Core MVC client)** and an **xUnit test suite**, all in one solution.

---

## 1. What is in the solution

```text
RaceDay/
├── .github/workflows/build-and-test.yml   # CI: restore, build, test, Docker image builds
├── docs/                                  # Part 1 design artefacts (ERD, endpoint plan, SQL schema)
├── src/
│   ├── RaceDay.Api/                       # Part 2 – RESTful Web API (.NET 8)
│   │   ├── Controllers/                   # Auth, Events, Categories, Enrolments, Participants,
│   │   │                                  # Results, Users, Media
│   │   ├── Data/                          # RaceDayDbContext (code-first) + DbInitializer (seed data)
│   │   ├── DTOs/                          # Request/response records (no entity cycles on the wire)
│   │   ├── Filters/                       # Session-based RequireAuth / RequireRole filters
│   │   ├── Models/                        # 7 entities: Organiser, Participant, Venue, Event,
│   │   │                                  # Category, Enrolment, Result
│   │   ├── Services/                      # PasswordService (PBKDF2), LocalFileService / LocalBlobService
│   │   ├── wwwroot/uploads/               # Runtime upload target (banners, avatars)
│   │   └── Program.cs                     # DI, EF Core, session, Swagger, static files, seed
│   └── RaceDay.Mvc/                       # Part 3 – ASP.NET Core MVC client (.NET 8)
│       ├── Controllers/                   # Account, Event, Organiser, Home
│       ├── Models/                        # View models + RaceDayUrls (API origin)
│       ├── Services/                      # IRaceDayApiClient / RaceDayClient + ApiSessionCookieHandler
│       ├── Views/                         # Razor views (Account, Event, Organiser, Participant, Shared)
│       ├── wwwroot/                       # CSS, JS, images (Cape Town banner), uploads
│       └── Program.cs                     # DI, session, HTTP client, invariant culture
├── tests/RaceDay.Tests/                   # xUnit suite (EF Core InMemory provider)
├── docker-compose.yml                     # Optional SQL Server + API + MVC containers
├── RaceDay.sln
└── README.md
```

Both applications target **.NET 8**, so the same SDK builds the API, the MVC client and the tests (the CI pipeline and both Dockerfiles also use .NET 8).

---

## 2. Prerequisites

| Requirement | Notes |
|---|---|
| .NET 8 SDK (or newer SDK able to target .NET 8) | `dotnet --version` should report 8.x or higher |
| SQL Server LocalDB | Default connection string `Server=(localdb)\mssqllocaldb;Database=RaceDayDb;Trusted_Connection=True;MultipleActiveResultSets=true`. Installed with Visual Studio. Any SQL Server instance works if you edit `src/RaceDay.Api/appsettings.json` |
| A modern browser | Chrome/Edge/Firefox |
| Optional: Docker Desktop | Only if you want to run `docker-compose.yml` |

There is no Azure or cloud dependency: banners and avatars are stored on local disk under `src/RaceDay.Api/wwwroot/uploads`.

---

## 3. Quick start (two terminals)

The MVC client talks to the API, so **start the API first**.

**Terminal 1 – API**

```powershell
dotnet run --project src/RaceDay.Api
```

* Listens on <http://localhost:5000>
* Swagger UI: <http://localhost:5000/swagger>
* On first start it creates `RaceDayDb`, seeds venues, events, categories and demo accounts, and closes any event whose date has already passed.

**Terminal 2 – MVC**

```powershell
dotnet run --project src/RaceDay.Mvc
```

* Listens on <http://localhost:5110>
* Open <http://localhost:5110> in the browser.

### Seeded accounts (password for all: `Password123!`)

| Role | Email |
|---|---|
| Organiser | `thabo@runsa.co.za` |
| Organiser | `sarah@capecycling.co.za` |
| Participant | `nomsa.dlamini@gmail.com` |
| Participant | `pieter.botha@gmail.com` |

### Reset the demo data

Stop the API, then:

```powershell
sqlcmd -S "(localdb)\mssqllocaldb" -Q "DROP DATABASE RaceDayDb;"
```

Delete `src/RaceDay.Api/wwwroot/uploads/*` to clear uploaded banners, then start the API again: the database is recreated and reseeded automatically.

---

## 4. Running the automated tests

The test project (`tests/RaceDay.Tests`) uses **xUnit** with the **EF Core InMemory provider**, so it needs no database server and no running API.

```powershell
# Whole solution (also compiles everything first)
dotnet test RaceDay.sln

# Only the tests, with per-test output
dotnet test tests/RaceDay.Tests/RaceDay.Tests.csproj --logger "console;verbosity=normal"

# Release configuration, as the CI pipeline runs them
dotnet test RaceDay.sln --configuration Release
```

Expected result:

```text
Passed! - Failed: 0, Passed: 6, Skipped: 0, Total: 6
```

You can also run them from Visual Studio with **Test → Test Explorer → Run All**, or with `dotnet test --collect:"XPlat Code Coverage"` for coverage.

### What the six tests cover

| Test | Verifies |
|---|---|
| `Test1_PublicGetEvents_ReturnsAllEventsWithDtoProjections` | Public event listing returns DTO projections (no entity cycles) |
| `Test2_RegisterDuplicateEmail_Returns409Conflict` | Duplicate registration is rejected with **409** |
| `Test3_OrganiserCreatesEvent_BindsOwnershipAndReturns201` | Event creation binds the logged-in organiser as owner (**201**) |
| `Test4_ParticipantEnrolment_AllocatesBibAndReturns201` | Enrolment generates a bib number and returns **201** |
| `Test5_DuplicateEnrolment_Returns409Conflict` | Re-enrolling in the same category returns **409** |
| `Test6_CategoryCapacityExceeded_ReturnsCapacityConflict` | A full category rejects further enrolments |

The controllers are constructed directly in the tests with a fake session (`SetFakeSession`) and an in-memory context, and `NullBlobService` stands in for file storage when building `EventsController`.

### Continuous integration

`.github/workflows/build-and-test.yml` runs on every push/PR to `main`:
restore → build the whole solution in Release → run all xUnit tests → build both Docker images.
A successful run is the "green build" evidence for the CI criterion (`docs/ci-build.png`).

---

## 5. Code structure and request flow

The three projects have one job each:

```text
Browser
   │  HTTP form post / link
   ▼
RaceDay.Mvc  ── Controller action ──► IRaceDayApiClient (RaceDayClient)
   │                                        │  HttpClient + API session cookie
   │                                        ▼
   │                                   RaceDay.Api ── Controller ── DbContext ── SQL Server
   │                                        │
   └──────────── Razor view ◄───────────────┘  (JSON DTOs deserialised into view models)
```

### Part 2 – API (`src/RaceDay.Api`)

* **`Program.cs`** – registers EF Core (`UseSqlServer`), session state, the password and file services, Swagger, static files for `/uploads`, creates `wwwroot` if missing, seeds the database and closes past events at startup.
* **`Controllers/`** – one controller per resource. Every write checks the session role (`HttpContext.Session.GetString("Role")`) and, where relevant, ownership (`OrganiserID == userId`).
* **`DTOs/ApiDtos.cs`** – `record` request/response shapes. Returning DTOs instead of EF entities prevents `Category → Enrolments → Category` serialisation cycles.
* **`Models/Entities.cs`** – the seven code-first entities and their relationships (unique index on `ParticipantID + CategoryID`, one result per enrolment).
* **`Data/DbInitializer.cs`** – deterministic seed data: three venues, three events, seven categories, demo enrolments and results.
* **`Services/PasswordService.cs`** – PBKDF2 hashing with per-password salt, verified on login.
* **`Services/LocalFileService.cs` / `LocalBlobService.cs`** – saves uploads under `wwwroot/uploads/<folder>` and deletes files when a banner is replaced or removed.

### Part 3 – MVC (`src/RaceDay.Mvc`)

* **`Controllers/AccountController.cs`** – login/register/logout; stores `Role`, `UserRole`, `UserFullName` and `UserId` in session and redirects to the role dashboard.
* **`Controllers/EventController.cs`** – public catalogue, event details and the enrol action.
* **`Controllers/OrganiserController.cs`** – organiser dashboard, create/edit/delete event, category management, roster, result capture and CSV export. `LoadOwnedEventAsync` centralises the "organiser owns this event" check.
* **`Services/RaceDayClient.cs`** – typed `HttpClient` that maps each screen action onto an API endpoint and turns API error JSON into user-facing messages.
* **`Services/ApiSessionCookieHandler.cs`** – keeps the API's session cookie inside the MVC user's own session, so each browser session acts as its own API identity. The primary handler's shared cookie jar is disabled to stop one user's session leaking into another's request.
* **`Models/ViewModels.cs`** – view models with data annotations; `EventSummaryViewModel.IsRegistrationOpen` and `BannerImageUrlResolved` keep view logic out of the Razor markup.
* **`Views/`** – Razor views with Bootstrap; `_Layout.cshtml` renders the shared navigation, the `TempData` success/error alerts, and a small script that confirms destructive forms and disables submit buttons while a request is in flight.

### Security and business rules implemented end to end

* Session-based authentication; organisers and participants have separate capabilities (wrong role → 403).
* Ownership checks on edit/delete event, category changes, rosters, result capture and CSV export.
* Unique enrolment per participant/category; capacity limits; bib numbers generated server-side (`BIB-{event}-{category}-{random}`).
* Events close automatically once their date passes; past or closed events reject enrolment and show "Registration closed" instead of an enrol button.
* Past dates cannot be used when creating an event (validated in MVC and again in the API).
* Duplicate category names within an event are rejected (case-insensitive) on create and rename.
* Deleting an event or category that already has enrolments is refused with **409** instead of cascading data loss.
* Uploaded banners are validated (JPG/PNG/WebP, max 5 MB), replaced files are deleted, and deleting an event removes its banner file.
* CSV export quotes and escapes fields and neutralises spreadsheet formula injection.
* Antiforgery tokens on every form; API errors are surfaced to the user instead of being swallowed.

---

## 6. Feature walkthrough

### Public
* **Home / Events / Catalogue** – browse and search events by name, city or category; each card shows its banner.
* **Event details** – full description, venue, categories with distance, fee and remaining capacity.

### Participant (athlete)
* Register or log in, then land on the athlete dashboard.
* Dashboard lists my enrolments (event, category, bib, status) and my results (bib, finish time, position).
* Enrol in a category: the API re-checks capacity and duplicates, generates the bib and returns a clear message ("Registration closed", "already enrolled", "capacity reached").
* Profile page showing contact and emergency contact details (read-only in the MVC client). Full profile update and avatar upload are exposed by the API (`PUT api/users/{id}/profile`, `POST api/media/upload-profile-picture`).

### Organiser
* Dashboard shows every event as a card with banner, plus **Preview**, **Edit**, **Categories**, **Roster** and **Delete**.
* **Create event** – details, one or more race categories (add/remove rows) and an optional banner upload.
* **Edit event** – change name, date, start time, venue, status and description; replace or remove the banner.
* **Manage categories** – add, rename, re-price, change capacity; entered counts are shown; categories with enrolments cannot be deleted.
* **Roster & results** – view all entrants per event, capture or update finish time and position (format `hh:mm:ss`), and export the roster/results as CSV.
* **Delete event** – confirmation dialog, double-submit protection, refused when enrolments exist.

### API endpoints (summary)

| Method | Route | Role |
|---|---|---|
| POST | `api/auth/register`, `api/auth/login`, `api/auth/logout` | Public |
| GET | `api/events?search=`, `api/events/{id}`, `api/events/{id}/categories`, `api/events/{id}/results` | Public |
| POST / PUT / DELETE | `api/events`, `api/events/{id}` | Organiser (owner for PUT/DELETE) |
| GET | `api/events/{id}/enrolments` | Organiser (owner) |
| POST | `api/events/{eventId}/enrol` | Participant |
| GET / DELETE | `api/enrolments/my`, `api/enrolments/{id}` | Participant (own) |
| POST | `api/events/{id}/categories` | Organiser (owner) |
| PUT / DELETE | `api/categories/{id}` | Organiser (owner) |
| POST | `api/results` | Organiser (event owner) |
| GET | `api/results/me` | Participant |
| GET | `api/participants/profile`, `api/participants/enrolments`, `api/participants/results` | Participant |
| POST / DELETE | `api/media/upload-event-banner/{id}`, `api/media/event-banner/{id}`, `api/media/upload-profile-picture` | Organiser / Participant |
| PUT | `api/users/{id}/profile` | Participant |

Swagger at <http://localhost:5000/swagger> documents the request and response shapes.

---

## 7. Suggested demonstration video script

The rubric asks for the video to explain the **code structure** and cover what the application does in detail, so this outline is deliberately paced (roughly 10–12 minutes) instead of clicking through screens quickly.

| Time | Section | Talk through |
|---|---|---|
| 0:00–0:45 | Introduction | What RaceDay does, the three roles (public, participant, organiser), which projects make up the solution and where they live |
| 0:45–2:00 | Running it | Show the two terminals (`dotnet run` for API then MVC), the ports, Swagger at 5000, and the MVC home page. Mention that the API creates and seeds the LocalDB database on first run |
| 2:00–4:00 | Code structure | Walk the solution tree: `Controllers` → `DTOs` → `DbContext` → `Models`; then the MVC side: `EventController` → `IRaceDayApiClient` → API controller → EF Core. Open `Program.cs` in both projects and explain DI, session, and the typed `HttpClient`. Open `ApiSessionCookieHandler` and explain why the API session must travel with each MVC user |
| 4:00–6:00 | Participant journey | Register/login, dashboard, browse catalogue, open an event, enrol (show the generated bib), show the duplicate/capacity/closed-event messages, show results on the dashboard |
| 6:00–8:30 | Organiser journey | Create an event with two categories and a banner, edit it, manage categories (duplicate-name rejection, delete rules), open the roster, capture a result, update it, export the CSV and open it |
| 8:30–10:00 | Rules and security | Point at the role checks and ownership comparisons in `OrganiserController` and the API controllers; show a participant hitting an organiser URL and being refused; show a closed event hiding the enrol form; show delete being refused when enrolments exist |
| 10:00–11:30 | Tests and CI | Run `dotnet test RaceDay.sln`, explain that the tests use EF Core InMemory and a fake session, name what each test asserts, then show the GitHub Actions run (restore → build → test → Docker images) |
| 11:30–12:00 | Wrap-up | Summarise the architecture and the rules enforced end to end; mention where to find the README and design documents |

Practical tips: record at 1080p with microphone audio, keep a terminal and Visual Studio/VS Code side by side, zoom the editor font, and narrate *why* each check exists rather than only clicking.

---

## 8. Troubleshooting

| Symptom | Cause / fix |
|---|---|
| `Failed to connect to http://localhost:5000` | Start the API before the MVC project, and check nothing else owns port 5000 |
| `MSB3027`/`MSB3021` "file is locked" on rebuild | The app is still running; stop it (Ctrl+C) before building again |
| Logged in, but organiser actions fail with 403 | The API was restarted, so its in-memory session was lost; log in again |
| Uploaded banner shows as broken image | The API must be running and serving `/uploads`; the file lives under `src/RaceDay.Api/wwwroot/uploads` |
| `(localdb)` connection error | LocalDB is not installed; install SQL Server Express/LocalDB or point `DefaultConnection` at another server |
| Data looks like old test data | Drop `RaceDayDb` (see section 3) and restart the API to reseed |
| Event shows "Registration closed" | Its date has passed, or its status is Closed; edit the date/status as the organiser |

Note: event closing compares the **UTC** date, so an event dated today can stay open for up to two hours after local midnight in South Africa (UTC+2).

---

## 9. Optional: Docker

`docker-compose.yml` starts SQL Server 2022, the API (port 5000) and the MVC client (port 5050). The MVC reads `ApiSettings__BaseUrl` from the compose file, which points at the API container.

```powershell
docker compose up --build
```

The same two Dockerfiles are built by the CI pipeline to prove they compile.

---

## 10. Submission checklist

* [ ] Project ZIP contains `src/`, `tests/`, `docs/`, `RaceDay.sln`, `README.md`, `docker-compose.yml` and the Dockerfiles. Do **not** include `bin/`, `obj/`, `.vs/` or uploaded test images.
* [ ] ZIP opens and extracts cleanly.
* [ ] GitHub repository has the latest commit(s) with both the Part 2 API and Part 3 MVC projects.
* [ ] Lecturer (`dphiri@rosebankcollege.co.za`) added as a repository collaborator.
* [ ] Demonstration video uploaded (unlisted YouTube link that plays without permissions, or ARC), with audible narration covering the code structure and each feature.
* [ ] `dotnet test RaceDay.sln` passes locally before submitting.

---

## 11. Video walkthroughs

* Part 1 (design, ERD, endpoint planning): <https://youtu.be/K1jBfWLmOd8>
* Part 2 (API, Swagger, security boundaries, tests): <https://youtu.be/KOAxe6X7Ubs>
* Part 3 (MVC client, end-to-end flows, code structure): _add the link here before submitting_
