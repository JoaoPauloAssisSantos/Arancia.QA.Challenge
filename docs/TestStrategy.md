1. Introduction
This document outlines the testing strategy for the Restful-Booker Platform, a full-stack application for hotel reservation management. The goal is to ensure business logic reliability (API) and a seamless user experience (UI) through a Shift-Left approach.
2. Scope & Assumptions
In-Scope: Authentication flows, Booking management (Create/Read/Delete), and Admin dashboard interactions.
Out-of-Scope: Load testing, payment gateway simulations, and cross-browser testing for legacy browsers (IE).
Assumptions: The demo environment is stable and the API follows RESTful standards.
3. Identified Risks & Mitigation
Data Flakiness: Parallel tests might interfere with shared resources (e.g., deleting a booking another test is reading).
Mitigation: Use unique data for each test run and isolated sessions.
Environment Stability: Being an open-source demo, the server might go down.
Mitigation: Implement a "Health Check" test as the first step of the CI/CD pipeline.
Token Expiration: Administrative tasks require a session cookie.
Mitigation: Implement an automated Auth-helper to refresh tokens before secure tests.
4. Testing Pyramid & Prioritization
Following the Testing Pyramid, the effort will be distributed as follows:
API Tests (70%): Validating all HTTP status codes, JSON schemas, and business rules (e.g., mandatory fields).
UI Tests (30%): Validating the "Golden Path" (End-to-End) from the customer's perspective and the Admin's login flow.
Rationale: API tests provide faster feedback and are less expensive to maintain.
5. Tools & Frameworks
Language: C# (.NET 8/10)
Backend Testing: RestSharp + xUnit + FluentAssertions.
Frontend Testing: Selenium WebDriver (chosen for its industry-standard stability and POM support).
CI/CD: GitHub Actions.
Documentation: Markdown for test cases and bug reports.
6. Test Scenarios & Coverage Matrix
The following scenarios represent the core business logic and critical user journeys. Detailed step-by-step instructions for each can be found in the [ManualTestCases.md](./ManualTestCases.md) file.

| ID | Component | Scenario Description | Priority | Automation Status |
|:---|:---|:---|:---|:---|
| **API-01** | Auth | Validate successful admin login and token generation | P1 (Critical) | Automated |
| **API-02** | Booking | Create a new booking with valid dynamic data | P1 (Critical) | Automated |
| **API-03** | Booking | Attempt to delete a booking without a valid token | P2 (High) | Automated |
| **UI-01** | Booking | Complete room booking flow via homepage | P1 (Critical) | Automated |
| **UI-02** | Admin | Admin login and message inbox validation | P2 (High) | Manual |
| **UI-03** | Contact | Validate mandatory fields in the contact form | P3 (Medium) | Manual |