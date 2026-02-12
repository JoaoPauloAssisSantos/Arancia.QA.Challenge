# 🏨 QA Technical Challenge - Restful Booker Platform

This repository contains a comprehensive automated testing suite for the [Restful Booker Platform](https://automationintesting.online), covering both **Backend (API)** and **Frontend (UI)**. 

The project was developed as part of a technical selection process, focusing on **C# (.NET 10)**, **Selenium**, and **RestSharp**.

---

## 🎯 Project Objective
The goal is to ensure business logic reliability and a seamless user experience through a **Shift-Left** testing approach. The suite validates the "Golden Path" (End-to-End) and critical negative scenarios, following the **Testing Pyramid** principles.

## 🛠 Tech Stack & Rationale
- **Language:** C# (.NET 10) - *Leveraging the latest features of the .NET ecosystem.*
- **API Testing:** [RestSharp](https://restsharp.dev) + [FluentAssertions](https://fluentassertions.com) - *Chosen for high readability and robust HTTP client capabilities.*
- **UI Testing:** [Selenium WebDriver](https://www.selenium.dev) - *The industry standard for stability and support for the Page Object Model (POM).*
- **Test Runner:** xUnit - *Modern, extensible, and developer-friendly.*
- **Data Generation:** [Bogus](https://github.com) - *Ensures unique and realistic test data for every execution.*

---

## 📂 Repository Structure
```text
├── .github/workflows/    # CI/CD Pipeline (GitHub Actions)
├── docs/                 # Documentation deliverables
│   ├── TestStrategy.md   # Scope, Risks, and Rationale (Item 1)
│   ├── ManualTests.md    # Detailed Manual Scenarios (Item 2)
│   └── BugReports.md     # Documented issues found (Item 4)
├── src/
│   ├── API.Tests/        # RestSharp implementation (Item 3)
│   ├── UI.Tests/         # Selenium E2E Tests (Item 3)
│   └── Framework/        # Shared Page Objects, Helpers, and Drivers
└── README.md             # Project overview and setup


____


 Test Strategy Highlights
Health Checks: Every execution starts by verifying environment availability to prevent "false negatives."
Atomic Tests: Due to the environment's 10-minute automated reset, tests are self-contained, handling their own data setup (creation) and teardown.
Page Object Model (POM): UI tests use POM to ensure maintainability and reduce code duplication.
⚠️ Environmental Constraints & Known Issues
During the testing phase, the following observations were made:
Auto-Reset: The demo environment resets its database every 10 minutes.
BUG-004 (Sync Issue): A critical bug was identified where new rooms created in the Admin panel do not immediately appear on the public Homepage.
Note: Automated UI tests include assertions that document and track this behavior.
⚙️ How to Run the Project
Prerequisites
.NET 10 SDK installed.
Google Chrome (latest version) and ChromeDriver.
Steps
Clone the repository:
bash
git clone https://github.com
cd your-repo-name
Use o código com cuidado.

Restore dependencies:
bash
dotnet restore
Use o código com cuidado.

Execute all tests:
bash
dotnet test
Use o código com cuidado.

Generate Reports (Optional):
If you have Allure configured, run:
bash
allure serve allure-results