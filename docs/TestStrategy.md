# Test Strategy — Restful-Booker Platform

## 1. Introduction

This document describes the testing strategy for the Restful-Booker Platform, a full-stack application for hotel reservation management. The primary goals are:

- Ensure **backend business rules** are correct and robust (API),
- Ensure a **seamless and secure user experience** (UI),
- Apply a **Shift-Left** approach (more tests close to the code, earlier feedback).

---

## 2. Scope & Assumptions

**In Scope**

- **Authentication flows**
  - `/auth` (RESTful Booker API)
  - Admin login/logout on `https://automationintesting.online`
- **Booking management**
  - Create / Read / Update / Delete (CRUD) for bookings
  - Negative and edge cases (invalid data, missing auth, conflicts, large payloads)
- **Room management (Admin)**
  - Create / Read / Update / Delete rooms via API (`/api/room`) and UI (`/admin/rooms`)
- **Reports / Integrations**
  - Visibility of API-created bookings in UI (Admin Report)
  - UI ↔ API consistency

**Out of Scope (for this iteration)**

- Load and stress testing
- Payment gateway simulations
- Cross-browser testing for legacy browsers (e.g., Internet Explorer)
- Comprehensive mobile device matrix (only basic responsive checks)

**Assumptions**

- The demo environments are reasonably stable (known to be “best effort”).
- APIs follow the documented RESTful contracts.
- Authentication and authorization are enforced consistently across UI and API.

---

## 3. Risks & Mitigations

**Data flakiness**

- *Risk*: Parallel tests or repeated executions may conflict on shared data (e.g., trying to create a booking for the same room and dates, or deleting a resource another test expects).
- *Mitigation*:
  - Use **randomized or unique test data** (e.g., future dates, random room numbers).
  - Make tests **atomic** (each test sets up and tears down its own data).
  - Centralize cleanup logic (best-effort DELETE via API).

**Environment instability**

- *Risk*: As an open demo, the environment (Heroku / automationintesting) can go down or become slow.
- *Mitigation*:
  - Implement a **Health Check (API-01)** as the first step of the CI pipeline.
  - Fail fast if `/ping` or the home page are not reachable.
  - Log and report environment-related failures separately from functional ones.

**Token / session expiration**

- *Risk*: Admin actions (rooms, report, delete) require a valid cookie/token; expired sessions can cause sporadic failures.
- *Mitigation*:
  - Implement an **Auth helper** that obtains/refreshes tokens (API-08).
  - In UI tests, centralize login via a POM (`AdminAuthPage`) and **validate logout/session invalidation** (UI-08).

**Security / input handling**

- *Risk*: XSS or injection via text fields (both API and UI).
- *Mitigation*:
  - API tests for injection-like payloads (API-18).
  - UI tests validating **special characters and scripts are treated as text** (UI-09).

---

## 4. Testing Pyramid & Prioritization

The effort is distributed according to the Testing Pyramid:

- **API Tests (~70%)**
  - HTTP status codes for happy path and negative cases,
  - Validation of JSON schema / contracts,
  - Business rules (mandatory fields, length, types, room/booking conflict logic).
- **UI Tests (~30%)**
  - “Golden Path” end-to-end bookings via homepage,
  - Admin flows (login, logout, room management),
  - Critical negative cases (invalid forms, unauthorized access, injection handling),
  - Key integration checks (API ↔ UI consistency).

**Rationale**

- API tests are **faster, more deterministic and cheaper** to maintain.
- UI tests focus on **end-to-end journeys and visual behavior**, not exhaustive coverage of every input combination.

---

## 5. Tools & Frameworks

- **Language**: C# (.NET 8/10)
- **API Testing**: RestSharp, xUnit, FluentAssertions
- **UI Testing**: Selenium WebDriver (with Page Object Model)
- **Random Data**: Bogus
- **CI/CD**: GitHub Actions (with health check stage)
- **Documentation**: Markdown (`TestStrategy.md`, `ManualTests.md`, `BugReports.md`)

---
## 6. Test Plan

- ID: TP-001
- Objective: Validate core booking and admin flows (API + UI) and ensure no regressions across releases.
- Entry criteria: /ping returns 201; test env reachable; admin credentials available.
- Exit criteria: All P1 tests passed; no open P1 defects; regression suite executed.
- Test types: API (automated), UI (automated + manual), Integration, Security smoke.
- Environment: https://restful-booker.herokuapp.com (API), https://automationintesting.online (UI). DB resets every 10 min — tests must be atomic.
- Schedule: Smoke (every run), Full regression (nightly CI), Manual exploratory (ad‑hoc during triage).
- Roles: QA Engineer (test execution & automation), Dev (bug fixes), PO (acceptance).
- Deliverables: Test results, Allure/JUnit reports, BugReports.md, Release sign-off.
- Risks: env instability, flaky tests — mitigations: health-check, unique test data, retries where appropriate.
## 7. Test Scenarios & Coverage Matrix (Summary)

The table below summarizes the **key** automated and manual scenarios. Detailed steps and expected results are in [`ManualTests.md`](./ManualTests.md).

### 7.1 API Scenarios (RESTful Booker)

| ID       | Component | Scenario Description                                               | Priority           | Automation |
|---------:|-----------|--------------------------------------------------------------------|--------------------|-----------|
| API-01   | Health    | `/ping` returns expected status and non-empty body                 | P1 (High)          | Auto      |
| API-02   | Booking   | Create booking (happy path) with valid JSON                        | P1 (High)          | Auto      |
| API-03   | Booking   | Create booking missing required fields (firstname/bookingdates)    | P1 (High)          | Auto      |
| API-04   | Booking   | Create booking with invalid types (e.g. `totalprice: "abc"`)      | P2 (Medium)        | Auto      |
| API-05   | Booking   | Get list of bookings                                               | P1 (High)          | Auto      |
| API-06   | Booking   | Get booking by existing `bookingid`                                | P1 (High)          | Auto      |
| API-07   | Booking   | Get booking by non-existing ID                                     | P2 (Medium)        | Auto      |
| API-08   | Auth      | Auth token generation with valid credentials                       | P1 (High)          | Auto      |
| API-09   | Auth      | Auth with invalid credentials (no token)                           | P1 (High)          | Auto      |
| API-10   | Booking   | PUT update with valid auth, followed by GET to verify              | P1 (High)          | Auto      |
| API-11   | Booking   | PUT update without auth (blocked, booking unchanged)               | P1 (High)          | Auto      |
| API-12   | Booking   | PATCH partial update with auth                                     | P2 (Medium)        | Auto      |
| API-14   | Booking   | DELETE booking with auth (DELETE + subsequent 404 on GET)          | P1 (High)          | Auto      |
| API-15   | Booking   | DELETE booking without auth (blocked)                              | P1 (High)          | Auto      |
| API-16   | Booking   | Concurrent PATCH updates to the same booking                       | P1 (High)          | Auto      |
| API-17   | Booking   | Large payloads (10k+ chars) in text fields                         | P2 (Medium)        | Auto      |
| API-18   | Booking   | Special characters / injection strings treated safely              | P1 (High)          | Auto      |
| API-19   | Booking   | Content-Type enforcement (wrong/missing Content-Type)              | P2 (Medium)        | Auto      |
| API-20   | Contract  | Schema/contract consistency for `/booking`, `/booking/{id}`, `/auth`| P1 (High)         | Auto      |

**Rooms API**

| ID       | Component | Scenario Description                                               | Priority           | Automation |
|---------:|-----------|--------------------------------------------------------------------|--------------------|-----------|
| API-21   | Room      | Create room with valid data (`POST /api/room`)                     | P2 (Medium)        | Auto      |
| API-22   | Room      | Create room with invalid data (missing/invalid fields)             | P1 (High)          | Auto      |
| API-23   | Room      | Create room then GET `/api/room/{roomid}`                          | P1 (High)          | Auto      |
| API-24   | Room      | Create room without valid auth (blocked)                           | P1 (High)          | Auto      |
| API-25   | Room      | GET room with invalid roomid (bad request)                         | P1 (High)          | Auto      |
| API-26   | Room      | PUT update room with valid auth + verify via GET                   | P1 (High)          | Auto      |
| API-27   | Room      | PUT update room without auth (blocked / 5xx depending on bug)      | P1 (High)          | Auto      |
| API-28   | Room      | PUT update room with invalid data (400 + errors)                   | P1 (High)          | Auto      |
| API-29   | Room      | DELETE room with valid auth                                        | P1 (High)          | Auto      |
| API-30   | Room      | DELETE room without auth (blocked)                                 | P1 (High)          | Auto      |
| API-31   | Room/Booking | Create room then create booking for that room                   | P1 (High)          | Auto      |
| API-32   | Room/Booking | Create + update room then create booking for updated room       | P1 (High)          | Auto      |
| API-33   | Booking   | Booking for a room already booked on same dates (`409 Conflict`)   | P1 (High)          | Auto      |

---

### 7.2 UI Scenarios (https://automationintesting.online)

| ID      | Component | Scenario Description                                                      | Priority       | Automation |
|:--------|:----------|:-------------------------------------------------------------------------|:--------------:|:----------:|
| UI-01   | Booking   | Homepage loads; header + booking form visible                            | P1 (High)      | Auto       |
| UI-02   | Booking   | Create booking via UI (happy path)                                       | P1 (High)      | Auto       |
| UI-03   | Booking   | Booking form required fields: empty/invalid values show errors           | P1 (High)      | Auto       |
| UI-04   | Booking   | Invalid date range (checkout before checkin) blocked                     | P1 (High)      | Auto       |
| UI-05   | Booking   | Booking for same room & same dates twice (UI error, no booking)         | P2 (Medium)    | Auto       |
| UI-06   | Admin     | Admin login with valid credentials (Rooms/Report menus visible)          | P1 (High)      | Auto       |
| UI-07   | Admin     | Admin login with invalid credentials (error, stays on login)             | P1 (High)      | Auto       |
| UI-08   | Admin     | Logout invalidates session; protected URL redirects to login             | P1 (High)      | Auto       |
| UI-09   | Security  | Special chars / scripts in user input treated as plain text (no XSS)     | P1 (High)      | Auto       |
| UI-10   | Security  | Unauthorized actions from UI (direct access to admin URLs) are blocked   | P1 (High)      | Auto       |
| UI-19   | Perf      | UI performance with many bookings present                                | P2 (Medium)    | Manual/Auto (TBD) |

---

### 7.3 Integration Scenarios (API ↔ UI)

| ID      | Component     | Scenario Description                                                                 | Priority  | Automation |
|:--------|:--------------|:--------------------------------------------------------------------------------------|:---------:|:----------:|
| INT-21  | API ↔ UI      | Booking created via `POST /booking` (API) is visible in Admin Report UI             | P1 (High) | Auto       |
| INT-22  | Admin Rooms   | Room created via `/admin/rooms` (UI) appears in Rooms API (`GET /room`)             | P1 (High) | Auto       |
| INT-23  | Admin Rooms   | Room created via `/api/room` (API) appears in `/admin/rooms` and/or Report UI       | P1 (High) | Auto       |

> **Note:** The exact IDs for integration tests in code are named `INT-xx`, but they are logically tied to existing API/UI behaviours (e.g., INT-21 = UI-11 in your notes).

---

## 8. Summary

- **API tests** cover detailed validation of contracts, edge cases, and concurrency for both bookings and rooms.
- **UI tests** validate the main user journeys (customer booking) and critical admin flows (rooms, report, security).
- **Integration tests** ensure that API-created data is reflected in the UI and that UI operations are consistent with the API (`GET /room`, `GET /booking`, Admin Report).

This strategy emphasizes **fast feedback via APIs**, with **targeted, robust UI and integration tests** to cover the most critical business and security paths.
