# RaceDay – Event Management Platform (Part 1)

RaceDay is a centralized, full-stack event management platform architected for South African road running, walking, and cycling events. It streamlines race operations by replacing manual entries with an integrated operational schema.

---

## System Roles & Access Control

* **Organiser:** Manages the operational event lifecycle. Organisers can create and edit events, define entry categories and quotas, view registered enrolments, and capture or update participant finish results.
* **Participant:** Interacts with the event catalog. Participants can register personal accounts, maintain profiles and emergency contact details, browse upcoming events, enter specific race categories, and monitor their results history.

---

## Planning Documentation

All system design and database planning artifacts are available in the `/docs` directory:
* **Entity Relationship Diagram (ERD):** `docs/erd-diagram.pdf`
* **REST API Endpoint Plan:** `docs/api-endpoint-plan.pdf`
* **SQL Database Script:** `docs/schema-init.sql`

---

## CI/CD Pipeline Verification

The repository utilizes GitHub Actions to validate the structure and presence of all required documentation on every push and pull request.

![CI/CD Build Status](docs/ci-build.png)

---

## Video Demonstration

* **Unlisted YouTube Walkthrough:** https://youtu.be/K1jBfWLmOd8
*(The video demonstrates ERD design choices, REST API endpoint planning, and an error-free top-to-bottom run of the SQL schema and verification queries in SSMS).*

# RaceDay – Event Management Platform (Part 2)

RaceDay is a centralized, RESTful Web API designed to manage road running, cycling, and trail-walking events across South Africa[cite: 1, 2]. The platform provides event creation, category management, automated race bib allocation, capacity enforcement, and official finish time tracking[cite: 1, 2].

Built with **ASP.NET Core (.NET 8)**, **Entity Framework Core Code-First**, and **SQL Server**, the system enforces server-side session authentication with PBKDF2 cryptographic password hashing and role-based access control[cite: 1, 2].

---

## 1. System Roles & Access Control

The API enforces strict role isolation via custom server-side session authorization filters (`RequireRoleAttribute`):

* **Event Organiser:**
  * Can register and authenticate to create, update, and cancel events.
  * Creates distance categories with entry fees, distances, and participant limits.
  * Inspects participant enrolment rosters.
  * Captures and updates official athlete finish times and positions.
  * *Restriction:* Cannot enrol as an athlete in race events (returns `403 Forbidden`).

* **Participant (Athlete):**
  * Registers and authenticates to manage personal profiles and emergency contact details.
  * Browses public upcoming events and category details.
  * Enrols into race categories, triggering automated bib number generation.
  * Withdraws from enrolled categories.
  * Views personal race performance and historical results.
  * *Restriction:* Cannot create, edit, or delete events or categories, and cannot capture finish results (returns `403 Forbidden`).

---

## 2. Project Architecture & Solution Structure

The project strictly follows a decoupled enterprise multi-layer architecture, maintaining clean isolation between production source code and test projects:

```text
RaceDay/
├── .github/
│   └── workflows/
│       └── build-and-test.yml       # GitHub Actions automated build & test pipeline
├── docs/
│   ├── ci-build.png                 # Verified GitHub Actions green build screenshot
│   ├── erd-diagram.png              # Entity Relationship Diagram (Part 1)
│   └── schema-init.sql              # Relational SQL schema
├── src/
│   └── RaceDay.Api/
│       ├── Controllers/             # REST API controllers with Swagger annotations
│       ├── Data/                    # EF Core DbContext and database seed initializers
│       ├── DTOs/                    # Request/Response DTOs eliminating cycle references
│       ├── Filters/                 # Custom session-based role authorization filters
│       ├── Models/                  # 7 Code-First domain entities
│       ├── Services/                # Cryptographic PBKDF2 password hashing service
│       ├── appsettings.json         # Connection strings and logging configuration
│       ├── Program.cs               # Middleware pipeline and Swagger configuration
│       └── RaceDay.Api.csproj
├── tests/
│   └── RaceDay.Tests/
│       ├── RaceDayApiTests.cs       # xUnit integration & unit test suite
│       └── RaceDay.Tests.csproj
├── README.md                        # Project documentation and walkthrough guide
└── RaceDay.sln                      # Visual Studio / .NET root solution file
