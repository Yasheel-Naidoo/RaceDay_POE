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
