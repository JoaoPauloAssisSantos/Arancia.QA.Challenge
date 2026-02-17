# 🏨 QA Technical Challenge — Restful Booker Platform

This repository contains an automated testing suite for the **Restful Booker Platform**, covering:

- **Backend (API)** — `https://restful-booker.herokuapp.com`
- **Frontend (UI)** — `https://automationintesting.online`

The project was developed as part of a technical selection process, using **C# (.NET)**, **RestSharp**, and **Selenium WebDriver**.

---

## 🎯 Project Objective

Ensure **business logic reliability** and a **seamless user experience** by:

- Focusing on **Shift-Left** testing (strong API coverage),
- Covering the **Golden Path** (end-to-end bookings) and **critical negative scenarios**,
- Following the **Testing Pyramid**: more API tests, fewer but meaningful UI tests.

---

## 🛠 Tech Stack & Rationale

- **Language:** C# (.NET 8/10)  
- **API Testing:** [RestSharp](https://restsharp.dev) + [FluentAssertions](https://fluentassertions.com)  
  - High readability, fluent assertions, clear error messages.
- **UI Testing:** [Selenium WebDriver](https://www.selenium.dev)  
  - Industry-standard, supports **Page Object Model (POM)** for maintainability.
- **Test Runner:** xUnit  
  - Modern, extensible, good .NET ecosystem integration.
- **Data Generation:** [Bogus](https://github.com/bchavez/Bogus)  
  - Realistic randomized data, avoids clashes between runs.
- **CI/CD:** GitHub Actions  
  - Runs API + UI tests on push/PR.
- **Documentation:** Markdown  
  - `docs/TestStrategy.md`, `docs/ManualTests.md`, `docs/BugReports.md`.

---

## 📂 Repository Structure

```text
├── .github/workflows/        # CI/CD Pipeline (GitHub Actions)
├── docs/
│   ├── TestStrategy.md       # Strategy: scope, risks, rationale
│   ├── ManualTests.md        # Detailed manual scenarios
│   └── BugReports.md         # Known issues & evidence
└── src/
    ├── API.Tests/            # RESTful Booker API tests (RestSharp + xUnit)
    └── UI.Tests/             # UI & integration tests (Selenium + POM)

📌 Test Strategy Highlights
Health Checks First
API tests always start with a simple /ping (API-01) to reduce false negatives when the environment is down.

Atomic Tests & Own Data
Each test:

Creates its own data (booking/room) via API or UI,
Validates it,
Cleans up via API (best-effort DELETE).
Page Object Model (POM)
UI tests use POM for:

HomePage (public booking),
RoomPage (booking details),
AdminAuthPage (admin login/logout),
AdminRoomsPage (room management),
AdminReportPage (calendar/report).
Integration Focus

A set of integration tests validates that:

A booking created via API appears in the Admin Report UI,
A room created via UI appears in the Rooms API, and vice-versa.
⚠️ Environment Constraints & Known Issues
Auto-Reset
The demo environment periodically resets its data (and may drop sessions). Tests are designed to set up all required data on each run.

UI vs API Out-of-Sync Behavior
Some known bugs are documented and asserted in tests (for example, inconsistent error handling or SPA crashes when the API returns certain 4xx/409 responses).
See docs/BugReports.md for details.

Flaky UI (SPA) on Some Error Paths
In some negative flows (e.g., conflicting bookings), the API behaves correctly (409 Conflict), but the SPA shows a generic "Application error".

UI tests:

Assert for the expected friendly error.
If not found, capture screenshot, HTML, and browser console logs for triage and fail the test.
⚙️ Setup Instructions

Prerequisites
.NET SDK (8.0)
Check with:

dotnet --version
Google Chrome (latest)
ChromeDriver
Either:
Install via NuGet (Selenium.WebDriver.ChromeDriver), or
Ensure chromedriver is on your PATH.
(Optional) Allure CLI for rich HTML reports:
npm install -g allure-commandline
# or download from https://github.com/allure-framework/allure2

Clone the Repository

git clone https://github.com/<your-org-or-user>/<your-repo-name>.git
cd <your-repo-name>
Restore Dependencies
From the repo root:
dotnet restore
This restores packages for all projects under src/.

▶️ How to Run the Tests
Run All Tests (API + UI)
From the repo root:

dotnet test

This will:

Build all projects under src/,
Run API tests (src/API.Tests/),
Run UI tests (src/UI.Tests/).

Run Only API Tests
dotnet test src/API.Tests/API.Tests.csproj
Run Only UI Tests

dotnet test src/UI.Tests/UI.Tests.csproj
Filter by Category / Trait (optional)
If tests are decorated with traits (e.g., [Trait("Type", "UI")]):

# only tests with Trait Type=UI
dotnet test --filter "Type=UI"

# only tests with Trait Type=API
dotnet test --filter "Type=API"
(Adjust if you’ve adopted custom traits.)

📊 Test Execution Notes
Parallelism  

API tests can run in parallel if needed.
UI tests typically run sequentially (class-level WebDriverFixture) to avoid WebDriver interference.
Base URLs / Environment Variables

The tests assume:

API: https://restful-booker.herokuapp.com
UI: https://automationintesting.online
You can override via environment variables (if wired in settings):

# Example (if used in Settings/UiSettings)
export UI_BASE_URL="https://automationintesting.online"
export API_BASE_URL="https://restful-booker.herokuapp.com"
Screenshots & Logs on Failure (UI)
For critical UI tests (booking, admin flows, integration):

Screenshots are saved to the test output folder (e.g., *.png).
Page HTML and browser console logs may also be stored for debugging.
📑 Reports (Optional — Allure)

If you have Allure configured in your CI or locally (test project generates Allure-compatible results):

Run tests and generate Allure results (e.g., allure-results folder).

Serve the report:

allure serve allure-results
This opens an interactive HTML report with:

Test history,
Attachments (screenshots, logs),
Suites and behaviors.

📝 Additional Notes

The suite intentionally covers both:
Positive (“happy path”) scenarios,
Negative / edge scenarios (invalid data, missing auth, conflicts, injection).
Some failing tests may represent real bugs in the demo environment (documented in docs/BugReports.md).
They are kept failing by design to serve as living documentation of current issues.
To disable tests that assert known bugs (for demo clarity), you can:
Use xUnit’s [Trait("KnownBug", "BUG-XXX")] and filter them out, or
Temporarily mark them with [Fact(Skip = "Known bug in demo env: BUG-XXX")].