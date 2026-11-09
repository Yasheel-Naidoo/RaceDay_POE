# RaceDay Part 3 — Demonstration Video Script

**Target length:** 13–14 minutes at a normal speaking pace (the marker's feedback asked for detail, not speed).
**Format:** 1080p, microphone audio, unlisted YouTube or ARC.
Square brackets `[...]` are screen or action cues — do not read them aloud. Bold text is what you say.

---

## Before you record (5-minute checklist)

1. Start the API first, then the MVC client, so both are already warm:
   ```powershell
   dotnet run --project src/RaceDay.Api      # terminal 1 → http://localhost:5000
   dotnet run --project src/RaceDay.Mvc      # terminal 2 → http://localhost:5110
   ```
2. Open these tabs in order: MVC home (`http://localhost:5110`), Swagger (`http://localhost:5000/swagger`), the green GitHub Actions run, and your editor with the solution loaded.
3. Log out of the MVC client so you start from a clean, signed-out state.
4. Have the credentials ready: organisers `thabo@runsa.co.za` / `sarah@capecycling.co.za`, participant `nomsa.dlamini@gmail.com`, all `Password123!`.
5. Increase the editor font size, close notifications, and record a 10-second audio test. Speak at about 130 words per minute — slower than you think.

---

## 0:00 – 0:55 · Introduction

[SCREEN: MVC home page]

**Good day. My name is Yasheel Naidoo, and this is Part 3 of my RaceDay POE — the MVC client.**

**RaceDay is an event management platform for road running, walking and cycling events in South Africa. It solves a very practical problem. Organisers currently take entries by email and keep them in spreadsheets, which makes bib numbers, category quotas and results error-prone and easy to lose.**

**The solution is two applications working together. Part 2 is a RESTful Web API built with ASP.NET Core, backed by SQL Server through Entity Framework Core. Part 3, which this video focuses on, is an ASP.NET Core MVC client that consumes that API. There is also an xUnit test project.**

**There are three kinds of user. The public can browse events. A participant can register, log in, enrol in an event and see their results. An organiser can create and manage events, categories, rosters and results.**

**In this video I'll show how to run the solution, walk through the code structure of both projects, demonstrate each feature at a normal pace, and finish with the automated tests and the CI pipeline.**

---

## 0:55 – 2:00 · Running the solution

[SCREEN: terminal 1]

**I'll start with how the solution is run, because the marker needs to be able to reproduce it. Everything targets .NET 8 — the API, the MVC client and the tests — so a single SDK builds the whole solution.**

**The first terminal runs the API with `dotnet run --project src/RaceDay.Api`. It listens on port 5000. On startup it does three things: it creates the LocalDB database if it doesn't exist, it seeds demo data only when the database is empty, and it closes any event whose date has already passed. You can see the seed and the closing sweep in the console.**

[SCREEN: browser → `http://localhost:5000/swagger`]

**Because it is a Web API, I can inspect it in Swagger. These are the endpoints the MVC client calls: auth, events, categories, enrolments, participants, results, users and media.**

[SCREEN: terminal 2]

**The second terminal runs the MVC client with `dotnet run --project src/RaceDay.Mvc`, on port 5110. The order matters. The API must be running first, because the MVC client is only a front end — it owns no database. If you open the MVC app without the API, the pages render but the data calls fail.**

[SCREEN: MVC home]

**This is the client. Before I use it, I'll show how the code is organised, so the features make sense when I demonstrate them.**

---

## 2:00 – 4:05 · API code structure

[SCREEN: Solution Explorer, expand all three projects]

**The solution has three projects: `RaceDay.Api`, `RaceDay.Mvc` and `RaceDay.Tests`.**

**On the API side, `Models/Entities.cs` defines seven entities: Organiser, Participant, Venue, Event, Category, Enrolment and Result. The relationships matter. An event belongs to an organiser and a venue. An event has many categories. An enrolment joins a participant to a category. A result belongs to an enrolment, one to one.**

**The enrolment has a unique index on participant plus category. That is the database-level guarantee that the same person cannot enter the same race twice, even if two requests arrive at the same moment.**

[SCREEN: `Data/RaceDayDbContext.cs`, then `Data/DbInitializer.cs`]

**`RaceDayDbContext` maps those entities. `DbInitializer` seeds three venues, three events, seven categories, demo participants, enrolments and results, so the app is never empty on a first run — which is why the marker can log in immediately.**

[SCREEN: `DTOs/ApiDtos.cs`]

**The controllers return DTOs from `ApiDtos.cs`, not entities. That is deliberate. Returning entities would serialise navigation properties and create cycles, and it would also let the client set fields it shouldn't. The DTOs are C# records, so the validation rules sit on the request shapes themselves.**

[SCREEN: `Controllers/EventsController.cs`]

**Let me show one controller in detail. In `EventsController`, the GET actions are public. The POST, PUT and DELETE actions first check the session role, then compare the event's `OrganiserID` with the logged-in user id. If they don't match, the organiser gets a 403 — they cannot edit somebody else's event, even if they guess the id.**

[SCREEN: `Controllers/EnrolmentsController.cs`]

**`EnrolmentsController` is where the enrolment rules live. On a request it loads the event and the category, rejects the request if the event is closed or its date has passed, rejects it if the category is full, rejects a duplicate enrolment, and only then creates the enrolment and generates a bib number.**

**The bib format is the event id, the category id and a random suffix. Because it is generated on the server, a client cannot choose its own bib.**

[SCREEN: `Program.cs` in the API]

**The API's `Program.cs` wires this together: EF Core with SQL Server, session state, the password service, the file service, Swagger, and static files so uploaded banners are served from `/uploads`. It also creates `wwwroot` at startup, so a fresh clone doesn't fail on the first upload.**

---

## 4:05 – 6:10 · MVC code structure

[SCREEN: `RaceDay.Mvc/Program.cs`]

**Now the MVC project, which is the Part 3 deliverable. `Program.cs` registers session, a typed HTTP client for the API, and the API base URL. That base URL comes from configuration: `appsettings.json` points at localhost port 5000 for development, and `docker-compose.yml` overrides it with the API container name. The same build therefore works locally and in Docker.**

[SCREEN: `Services/RaceDayClient.cs`]

**`Services/RaceDayClient.cs` is the only place that knows the API's routes. Each screen calls a method such as `GetEventsAsync`, `EnrolAsync`, `CreateEventAsync` or `SubmitResultAsync`, and every method goes through one shared error reader that turns an API error message into something a user can understand. If an API route changes, only this file changes.**

[SCREEN: `Services/ApiSessionCookieHandler.cs`]

**This next class is the most important design decision in the client. The API authenticates with a session cookie, but an MVC server is shared by every browser that uses it. If the HTTP client kept one shared cookie jar, one user's API session could be sent with another user's request.**

**`ApiSessionCookieHandler` instead stores the API's session cookie inside the MVC user's own session, and attaches it only to that user's outgoing calls. I also disabled the primary handler's cookie container so nothing is shared. That gives every browser its own identity at the API.**

[SCREEN: `Controllers/OrganiserController.cs`]

**Controllers are split by audience. `AccountController` handles login, register and logout, and stores the role, user id and display name in session. `EventController` serves the public catalogue and details. `OrganiserController` handles everything an organiser does.**

[SCREEN: highlight `LoadOwnedEventAsync`]

**The ownership check is a single helper, `LoadOwnedEventAsync`. Every organiser action — edit, delete, categories, roster, results and export — calls it first. If the event isn't yours, you get a 404, not somebody else's data. Centralising it means there is no screen where the check was forgotten.**

[SCREEN: `Models/ViewModels.cs`]

**`ViewModels.cs` holds the screen models and keeps decisions out of Razor. `IsRegistrationOpen` decides whether the enrol button or a "registration closed" notice appears, and `BannerImageUrlResolved` decides whether to show the bundled seed image or an uploaded banner served by the API.**

[SCREEN: `Views/Shared/_Layout.cshtml`]

**The views are organised per controller. The shared layout renders the navigation, the success and error alerts from `TempData`, and a small script that confirms destructive actions and disables submit buttons, so a double click can't submit a form twice.**

---

## 6:10 – 8:15 · Public and participant journey

[SCREEN: MVC home → Events page]

**Now the application itself. The home page and the Events page are public. Each card shows the name, date, venue, categories and a banner. Search filters by event name, city or category. I'll open the Cape Town Cycle Tour.**

[SCREEN: event details]

**Details shows the full description, the venue, and every category with its distance, entry fee and remaining places. Because this event takes place in 2027, registration is open and there is an enrol button.**

[SCREEN: log in as `nomsa.dlamini@gmail.com`]

**I'll log in as a participant, Nomsa. Registration and login are normal forms with validation. Passwords are hashed with PBKDF2 in the API — they are never stored in plain text.**

[SCREEN: participant dashboard]

**After login the client sends me to the participant dashboard. This lists my enrolments with the event, category, bib and status, and my results with the bib, finish time and position. The bibs here were generated by the API at the moment of enrolment.**

[SCREEN: enrol in a category]

**Let me enrol. I choose a category and submit, and the dashboard now shows a new enrolment with a generated bib number.**

**If I try the same category again, the API rejects it and the client shows "already enrolled". A full category reports that capacity has been reached. A closed event doesn't show the button at all. These are the same rules I pointed to in the API code, now demonstrated from the UI.**

---

## 8:15 – 11:00 · Organiser journey

[SCREEN: log out, log in as `thabo@runsa.co.za`]

**Now the organiser. I'll log in as Thabo. His dashboard lists only his events — that's the ownership rule applied to a list, not just to a single record.**

[SCREEN: Create event]

**I'll create an event slowly. I fill in the name, date, start time, venue and description. Then the categories: I use "Add category" to add a second row, and each row has a name, distance, entry fee and maximum participants. I attach a banner image.**

**When I save, the MVC client sends the fields and the banner to the API. The API validates the image type and size, stores the file under `wwwroot/uploads`, saves its URL, and the client redirects to the dashboard, where the new event appears with its banner.**

[SCREEN: Edit event]

**Edit loads the event's current values. I can change the date or the status, replace the banner, or tick remove to clear it. If I replace a banner, the API deletes the old file, so uploaded files don't accumulate.**

[SCREEN: Manage categories]

**Categories are managed per event. I can add a category, rename one, change its fee or its capacity. The API rejects a duplicate name within the same event — case-insensitively — and returns a conflict that the client displays. Categories that already have enrolments cannot be deleted. That is also a conflict, so no entrant is silently orphaned.**

[SCREEN: Roster and result capture]

**The roster shows every entrant for the event: participant, category, bib, enrolment status and any captured result. I'll capture a result — finish time as hours, minutes and seconds, plus a position — and save it. Reopening the roster shows the stored result, and updating it works the same way.**

[SCREEN: Export results]

**Export results downloads a CSV for this event. It quotes fields, escapes quotes, and prefixes values that begin with an equals sign, plus, minus or at sign, so a malicious name cannot turn into a spreadsheet formula.**

[SCREEN: Delete event]

**Finally, delete. It asks for confirmation first. If the event has enrolments, the API refuses with a conflict instead of cascading the data away. If the event is empty, the event and its banner file are removed.**

---

## 11:00 – 12:15 · Rules and security, demonstrated not claimed

[SCREEN: logged in as participant, request an organiser URL directly]

**Let me prove the boundaries rather than just describe them. I'm logged in as a participant, and I'll request an organiser URL directly. The MVC controller checks the role in session and refuses.**

**If I called the API directly with a participant session, its filters and ownership checks would refuse as well. The client is not the security boundary — the API is.**

**To summarise, these rules are enforced in the API and reflected in the UI:**

- **one enrolment per participant per category, enforced by a unique index as well as a check;**
- **category capacity;**
- **bib numbers generated only on the server;**
- **events closing on their date, with a startup sweep that closes past events on every launch;**
- **organiser actions restricted by role and by event ownership;**
- **past dates rejected when creating an event;**
- **uploads validated by type and size, with replaced files deleted.**

**None of these rules live only in JavaScript, so none of them can be bypassed from the browser.**

---

## 12:15 – 13:45 · Automated tests and CI

[SCREEN: terminal → `dotnet test RaceDay.sln`]

**The tests are xUnit, in `tests/RaceDay.Tests`. They use the EF Core InMemory provider, so they need no database and no running API, and each test builds its own context. The controllers are constructed directly, with a fake session, and a `NullBlobService` stands in for file storage.**

**Running `dotnet test RaceDay.sln` restores, builds and runs all six tests. All six pass.**

[SCREEN: test file with the six test methods visible]

**The six tests cover the paths most likely to break. One checks that the public event list returns DTO projections. One checks that duplicate registration returns a 409. One checks that event creation binds the organiser as owner and returns 201. One checks that enrolment generates a bib and returns 201. One checks the duplicate enrolment conflict. One checks the capacity conflict.**

**Together they cover the public read path, registration, event creation and the three enrolment rules.**

[SCREEN: GitHub Actions run, green]

**And this is the CI pipeline for the same commit. It runs on GitHub Actions: it installs .NET 8, restores, builds the whole solution in Release, runs the same test suite, and validates both Docker images. The run is green — build, tests and both container builds succeeded, which is the evidence for the CI criterion.**

---

## 13:45 – 14:15 · Close

[SCREEN: MVC home]

**To close: RaceDay is two applications and one test suite. The API owns the data and every business rule. The MVC client is a thin, typed client that keeps each user's API session separate and presents each role clearly.**

**The README explains setup, how the tests are run, the code structure, the endpoints and the submission checklist.**

**Thank you for watching.**

---

## Appendix: likely questions and short answers

If time allows, or if the marker asks:

- **Why DTOs instead of entities?** Serialising entities exposes navigation properties, creates cycles and lets clients post fields they shouldn't set. DTOs make the contract explicit and validate at the boundary.
- **Why does the MVC app need a custom cookie handler?** The API authenticates with a session cookie, and an MVC server is shared between users. The handler keeps each user's API session inside that user's MVC session so identities cannot cross.
- **Why is a bib generated on the server?** So it can't be chosen or tampered with, and so the format stays consistent for results and exports.
- **What stops a past event from taking entries?** Its status is derived from the date when it is read, a startup sweep closes past events, and the enrolment endpoint rejects closed or past events.
- **What happens on a fresh clone?** The API creates the database and seeds demo data when it is empty; banners and avatars live under `wwwroot/uploads`, which is created at startup and excluded from git.
- **Why both MVC-level and API-level ownership checks?** The MVC check gives a friendly 404 and keeps the UI honest; the API check is the real enforcement, because the API is reachable directly.
