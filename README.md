# RaceDay – Event Management Platform

RaceDay is a centralized, full-stack RESTful Web API and event management platform architected for South African road running, walking, and cycling events. It streamlines race operations by replacing manual entries with an integrated operational schema, automated bib allocation, quota enforcement, and role-based access control.

---

## System Overview & Roles

The system enforces strict role isolation via custom server-side session authorization filters (`RequireRoleAttribute`):

* **Event Organiser:** Manages the operational event lifecycle. Organisers can create and edit events, define entry categories and quotas, view registered enrolments, and capture or update participant finish results.
  * *Security Boundary:* Cannot enrol as an athlete in race events (returns `403 Forbidden`).
* **Participant (Athlete):** Interacts with the event catalog. Participants can register personal accounts, maintain profiles and emergency contact details, browse upcoming events, enter specific race categories, and monitor their results history.
  * *Security Boundary:* Cannot create, edit, or delete events or categories, and cannot capture finish results (returns `403 Forbidden`).

---

## Part 1: Design & Planning Documentation

All system design and database planning artifacts are available in the `/docs` directory:
* **Entity Relationship Diagram (ERD):** `docs/erd-diagram.png` (or `docs/erd-diagram.pdf`)
* **REST API Endpoint Plan:** `docs/api-endpoint-plan.pdf`
* **SQL Database Script:** `docs/schema-init.sql`

---

## Part 2: Backend Architecture & Solution Structure

Built with **ASP.NET Core (.NET 8)**, **Entity Framework Core Code-First**, and **SQL Server**, the solution adheres to an enterprise multi-layered architecture separating source code from automated test assemblies:

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
```
CI/CD Pipeline Verification
The repository utilizes GitHub Actions (.github/workflows/build-and-test.yml) to validate the solution on every push and pull request against ubuntu-latest. The workflow restores dependencies, compiles RaceDay.sln in Release mode, and runs all xUnit tests.

Video Demonstrations
Part 1 Video Walkthrough (Unlisted): https://youtu.be/K1jBfWLmOd8

(Demonstrates ERD design choices, REST API endpoint planning, and an error-free run of the SQL schema in SSMS).

Part 2 Video Walkthrough (Unlisted): [Insert Your Part 2 YouTube Link Here]

(Demonstrates Swagger UI endpoints, Organiser vs Participant security boundaries, live bib allocation, result capturing, and automated terminal unit tests).

Lecturer Evaluation & Testing Guide
This guide provides an end-to-end evaluation pathway to verify all functional requirements, security boundaries, database integrity rules, automated tests, and CI/CD pipelines.

1. Rapid Setup & Execution
Run the following commands sequentially in terminal/PowerShell from the repository root (RaceDay):

```Bash
# 1. Restore solution dependencies
dotnet restore RaceDay.sln

# 2. Build in Release mode
dotnet build RaceDay.sln --no-restore

# 3. Execute all automated xUnit tests
dotnet test RaceDay.sln --no-build

# 4. Launch the Web API
dotnet run --project src/RaceDay.Api
```
