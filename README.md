# 🏨 QA Technical Challenge — Restful Booker Platform

This repository contains an automated testing suite for the Restful Booker Platform:

- API: https://restful-booker.herokuapp.com  
- UI: https://automationintesting.online

Stack: C# (.NET 8), RestSharp, Selenium WebDriver, xUnit, FluentAssertions.

---

## 🎯 Goal
Provide reliable automated coverage for API and UI flows (golden path + critical negatives), with reproducible runs locally and in CI.

---

## 🔧 Prerequisites

- .NET SDK: 8.0 (verify with `dotnet --version`)  
- Google Chrome (latest stable)  
- ChromeDriver matching Chrome version (or use `Selenium.WebDriver.ChromeDriver` NuGet)  
- Optional: Allure CLI for HTML reports (`npm install -g allure-commandline`)

Environment recommendations:
- Run UI tests headless in CI.
- Use a runner with >= 2 CPU cores and 4 GB RAM for reasonable UI performance.

---

## 📁 Repo Layout
├── workflows/        # CI (GitHub Actions)
├── docs/
│   ├── TestStrategy.md
│   ├── ManualTests.md
│   └── BugReports.md
└── src/
    ├── API.Tests/
    └── UI.Tests/
README.md

---

## ⚙️ Configuration / Environment Variables

You can override base URLs and runtime flags via environment variables:

- `API_BASE_URL` (default: `https://restful-booker.herokuapp.com`)  
- `UI_BASE_URL` (default: `https://automationintesting.online`)  
- `CHROMEDRIVER_PATH` (optional) — path to chromedriver if not on `PATH`  
- `HEADLESS` (`true`|`false`) — run UI tests headless (recommended for CI)  
- `TEST_FILTER` — `dotnet test --filter` expression

Examples (Linux/macOS):

```bash
export API_BASE_URL="https://restful-booker.herokuapp.com"
export UI_BASE_URL="https://automationintesting.online"
export HEADLESS="true"

Windows (PowerShell):
$env:API_BASE_URL="https://restful-booker.herokuapp.com"
$env:UI_BASE_URL="https://automationintesting.online"
$env:HEADLESS="true"

▶️ How to Run
Restore dependencies:
dotnet restore

Run all tests (API + UI):
dotnet test

Run only API tests:
dotnet test src/API.Tests/API.Tests.csproj

Run only UI tests:
dotnet test src/UI.Tests/UI.Tests.csproj

Run a single test by DisplayName (example):
dotnet test --filter "DisplayName=UI-13*"

Notes:

Use HEADLESS=true in CI to avoid opening a browser.
If ChromeDriver is not on PATH, set CHROMEDRIVER_PATH to the executable directory.
🕒 Typical execution times
API test suite: ~30–90s (depends on parallelism)
UI tests (full suite): ~5–10 min (depends on number of UI tests and runner)
Adjust test timeouts if runner is slow.

🧩 Test design highlights
Health check (API-01) runs first to avoid false negatives on unavailable env.
Tests are atomic: create their own data and perform best-effort cleanup.
Page Object Model (POM) used for UI maintainability.
Randomized/unique test data used to avoid collisions (timestamped names).
Flaky/known-bug tests flagged with Trait KnownBug and documented in docs/BugReports.md.
📸 Artifacts & Reports
Screenshots and HTML saved on UI failures to test output folder (look for screenshot_*.png and page_*.html).
If Allure is configured, results are stored in allure-results/. Generate locally:
dotnet test
allure serve ./allure-results

CI: artifacts are uploaded by the workflow (see workflows/*).

♻ Cleanup & Isolation
Tests attempt best-effort cleanup (DELETE via API) in finally blocks.
Environment resets periodically — tests are designed to be resilient and re-create needed data.
🔁 CI (GitHub Actions)
The repo includes a workflow that:

Restores dependencies
Runs API tests
Runs UI tests (headless)
Uploads test results and artifacts
Notes:

UI tests may require a self-hosted runner if the hosted runner cannot run Chrome properly. Check workflows/*.yml for specifics.
Secrets (if any) should be stored in GitHub Secrets — do not hardcode tokens.
⚠ Known failing tests / environment caveats
Some tests document known issues in the demo environment (see docs/BugReports.md), e.g.:

BUG-001 — rooms created in Admin not immediately visible on public homepage.
BUG-002 — Booking conflict and invalid date range crash the SPA instead of showing a friendly error.
BUG-003 — Content-Type enforcement for POST /booking is too permissive.
BUG-004 — Update Room without valid Auth returns 500 with generic “unexpected error”.
Certain endpoints may return 500 after delete (server-side bug); tests use fallbacks to confirm deletion.

To exclude known-bug tests in a run, use trait filtering:
dotnet test --filter "TestCategory!=KnownBug"

✉ Contact / Author
João Paulo A. dos Santos
Repository: https://github.com/JoaoPauloAssisSantos/Arancia.QA.Challenge
Email: johux_ad@hotmail.com

