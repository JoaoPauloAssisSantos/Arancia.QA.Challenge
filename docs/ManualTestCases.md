# Test Cases — RESTful Booker (API + UI)

Base:
- API: https://restful-booker.herokuapp.com
- UI: https://automationintesting.online

---

## 1. API Test Cases (RESTful Booker)

### API-01 — Health Check (Ping)

| Field           | Description                                   |
|----------------|-----------------------------------------------|
| ID             | API-01                                        |
| Title          | Check API availability via `/ping`            |
| Precondition   | None                                          |
| Steps          | 1. Send `GET /ping`                           |
| Expected       | Status `201` (as per doc) and body not empty  |
| Severity       | High                                          |

---

### API-02 — Create booking (happy path)

| Field           | Description                                                                                                                  |
|----------------|------------------------------------------------------------------------------------------------------------------------------|
| ID             | API-02                                                                                                                       |
| Title          | Create a booking with valid data                                                                                             |
| Precondition   | None                                                                                                                         |
| Steps          | 1. Send `POST /booking` with a fully valid JSON body (all required fields).                                                  |
| Example body   | `{ "firstname": "John", "lastname": "Doe", "totalprice": 150, "depositpaid": true, "bookingdates": { "checkin": "2025-10-01", "checkout": "2025-10-05" }, "additionalneeds": "Breakfast" }` |
| Expected       | Status `200`/`201`. Response contains `bookingid` and a `booking` object mirroring sent data.                               |
| Severity       | High                                                                                                                         |

---

### API-03 — Create booking with missing required fields

| Field           | Description                                                                |
|----------------|----------------------------------------------------------------------------|
| ID             | API-03                                                                     |
| Title          | Reject booking creation when required fields are missing                   |
| Precondition   | None                                                                       |
| Steps          | 1. Send `POST /booking` without `firstname` or without `bookingdates`.    |
| Expected       | Status `400` (or other 4xx as per API). No booking created.               |
| Severity       | High                                                                       |

---

### API-04 — Create booking with invalid data types

| Field           | Description                                                                                           |
|----------------|-------------------------------------------------------------------------------------------------------|
| ID             | API-04                                                                                                |
| Title          | Reject booking creation with invalid data types                                                       |
| Precondition   | None                                                                                                  |
| Steps          | 1. Send `POST /booking` with `totalprice: "abc"` and `depositpaid: "notbool"`.                       |
| Expected       | 4xx response, clear error (no raw stack trace). Booking must not be created.                         |
| Severity       | Medium                                                                                                |

---

### API-05 — Get bookings list

| Field           | Description                                              |
|----------------|----------------------------------------------------------|
| ID             | API-05                                                   |
| Title          | Retrieve list of existing bookings                       |
| Precondition   | At least one booking exists (e.g., from API-02).        |
| Steps          | 1. Send `GET /booking`.                                  |
| Expected       | Status `200`. Response is an array of objects with `bookingid`. |
| Severity       | High                                                     |

---

### API-06 — Get booking by existing ID

| Field           | Description                                                                 |
|----------------|-----------------------------------------------------------------------------|
| ID             | API-06                                                                      |
| Title          | Retrieve booking details by existing ID                                     |
| Precondition   | One booking created, known `bookingid` (from API-02).                      |
| Steps          | 1. Send `GET /booking/{bookingid}`.                                        |
| Expected       | Status `200`. Response body contains all booking fields and correct values.|
| Severity       | High                                                                        |

---

### API-07 — Get booking by non-existing ID

| Field           | Description                                 |
|----------------|---------------------------------------------|
| ID             | API-07                                      |
| Title          | Handle request to non-existing booking ID   |
| Precondition   | Use a high number or guaranteed non-existing ID. |
| Steps          | 1. Send `GET /booking/99999999`.            |
| Expected       | Status `404` (or behavior defined by API).  |
| Severity       | Medium                                      |

---

### API-08 — Authentication — success

| Field           | Description                                                       |
|----------------|-------------------------------------------------------------------|
| ID             | API-08                                                            |
| Title          | Generate auth token with valid credentials                        |
| Precondition   | Valid username/password as per documentation.                     |
| Steps          | 1. Send `POST /auth` with `{ "username": "...", "password": "..." }`. |
| Expected       | Status `200`. Response contains non-empty `token` field.          |
| Severity       | High                                                              |

---

### API-09 — Authentication — invalid credentials

| Field           | Description                                                             |
|----------------|-------------------------------------------------------------------------|
| ID             | API-09                                                                  |
| Title          | Reject authentication with invalid credentials                          |
| Precondition   | None                                                                    |
| Steps          | 1. Send `POST /auth` with wrong password.                               |
| Expected       | Status `200` or `4xx` as per API; **no** `token` returned.             |
| Severity       | High                                                                    |

---

### API-10 — Update booking (PUT) — authenticated

| Field           | Description                                                                                                               |
|----------------|---------------------------------------------------------------------------------------------------------------------------|
| ID             | API-10                                                                                                                    |
| Title          | Fully update an existing booking (PUT) with valid auth                                                                    |
| Precondition   | (1) Booking exists (API-02) (2) Valid token (API-08).                                                                     |
| Steps          | 1. Send `PUT /booking/{id}` with complete updated JSON body + required auth header/cookie.                               |
| Expected       | Status `200`. Response body reflects updated data. A subsequent `GET /booking/{id}` returns the new values.              |
| Severity       | High                                                                                                                      |

---

### API-11 — Update booking (PUT) without authentication

| Field           | Description                                                |
|----------------|------------------------------------------------------------|
| ID             | API-11                                                     |
| Title          | Block booking update without authentication                |
| Precondition   | Booking exists.                                            |
| Steps          | 1. Send `PUT /booking/{id}` without auth token/cookie.    |
| Expected       | Status `403` / `401`. Booking remains unchanged.          |
| Severity       | High                                                       |

---

### API-12 — Partial update booking (PATCH) with auth

| Field           | Description                                                                                  |
|----------------|----------------------------------------------------------------------------------------------|
| ID             | API-12                                                                                       |
| Title          | Partially update booking (PATCH) with valid auth                                             |
| Precondition   | Booking exists + valid token.                                                                 |
| Steps          | 1. Send `PATCH /booking/{id}` with `{ "firstname": "UpdatedName" }` + auth.                  |
| Expected       | Status `200`. `firstname` updated; other fields remain unchanged.                            |
| Severity       | Medium                                                                                       |

---

### API-13 — Update booking with invalid ID

| Field           | Description                                                           |
|----------------|-----------------------------------------------------------------------|
| ID             | API-13                                                                |
| Title          | API should handle invalid booking ID for update                       |
| Precondition   | None                                                                  |
| Steps          | 1. Send `PUT /booking/abc` with a valid body.                         |
| Expected       | Status `404` or `400` as per API; no resource created/modified.       |
| Severity       | Low                                                                   |

---

### API-14 — Delete booking — authenticated

| Field           | Description                                                                                |
|----------------|--------------------------------------------------------------------------------------------|
| ID             | API-14                                                                                     |
| Title          | Delete a booking with valid authentication                                                 |
| Precondition   | Booking exists + valid token.                                                              |
| Steps          | 1. Send `DELETE /booking/{id}` with auth. <br> 2. Send `GET /booking/{id}` afterwards.    |
| Expected       | DELETE returns `201`/`200`. Subsequent GET returns `404`.                                  |
| Severity       | High                                                                                       |

---

### API-15 — Delete booking without authentication

| Field           | Description                                            |
|----------------|--------------------------------------------------------|
| ID             | API-15                                                 |
| Title          | Block booking deletion without authentication          |
| Precondition   | Booking exists.                                        |
| Steps          | 1. Send `DELETE /booking/{id}` **without** auth.      |
| Expected       | Status `403`/`401`. Booking remains accessible.       |
| Severity       | High                                                   |

---

### API-16 — Concurrent updates (synchronization)

| Field           | Description                                                                                                 |
|----------------|-------------------------------------------------------------------------------------------------------------|
| ID             | API-16                                                                                                      |
| Title          | Handle concurrent updates to the same booking                                                               |
| Precondition   | Booking exists; two valid tokens/sessions.                                                                  |
| Steps          | 1. From session A, `PATCH /booking/{id}` to set `firstname = "VersionA"`. <br> 2. From session B, soon after, `PATCH` to set `firstname = "VersionB"`. <br> 3. `GET /booking/{id}`. |
| Expected       | Final state is consistent (e.g., last update wins). No partial/corrupt data.                                |
| Severity       | High                                                                                                        |

---

### API-17 — Large payloads

| Field           | Description                                                                              |
|----------------|------------------------------------------------------------------------------------------|
| ID             | API-17                                                                                   |
| Title          | API behavior with very large string fields                                               |
| Precondition   | None                                                                                     |
| Steps          | 1. Create a large string (e.g., 10k+ chars) for `firstname` or `additionalneeds`. <br> 2. Send `POST /booking`. |
| Expected       | API responds with a controlled error (e.g., `400`/`413`) or properly accepts within limits. No crash/hang. |
| Severity       | Medium                                                                                   |

---

### API-18 — Special characters and injection strings

| Field           | Description                                                                                   |
|----------------|-----------------------------------------------------------------------------------------------|
| ID             | API-18                                                                                        |
| Title          | Handle special characters / potential injection safely                                        |
| Precondition   | None                                                                                          |
| Steps          | 1. Send `POST /booking` with `<script>alert(1)</script>` or SQL-like strings in text fields. |
| Expected       | Booking is treated as plain text; no execution of code; no security errors.                  |
| Severity       | High                                                                                          |

---

### API-19 — Content-Type enforcement

| Field           | Description                                                           |
|----------------|-----------------------------------------------------------------------|
| ID             | API-19                                                                |
| Title          | Reject requests with incorrect or missing Content-Type                |
| Precondition   | None                                                                  |
| Steps          | 1. Send `POST /booking` with `Content-Type: text/plain` or missing header. |
| Expected       | 4xx (e.g., `415`/`400`). No booking is created.                       |
| Severity       | Medium                                                                |

---

### API-20 — Response schema/contract consistency

| Field           | Description                                                                                  |
|----------------|----------------------------------------------------------------------------------------------|
| ID             | API-20                                                                                       |
| Title          | Validate that API responses follow the expected schema                                       |
| Precondition   | None                                                                                         |
| Steps          | 1. Call main endpoints (`/booking`, `/booking/{id}`, `/auth`). <br> 2. Verify presence and type of each field against API doc. |
| Expected       | All responses match documented schema. No missing/extra unexpected fields breaking contract. |
| Severity       | High                                                                                         |

---

## 2. UI Test Cases (https://automationintesting.online)

> Note: exact selectors/flows may change; adapt to actual DOM and behavior.

### UI-01 — Load homepage

| Field           | Description                                           |
|----------------|-------------------------------------------------------|
| ID             | UI-01                                                 |
| Title          | Homepage loads successfully                           |
| Precondition   | Application is up and reachable.                      |
| Steps          | 1. Open `https://automationintesting.online`.        |
| Expected       | Page loads without errors; main header and booking form visible. |
| Severity       | High                                                  |

---

### UI-02 — Create booking via UI (happy path)

| Field           | Description                                                                                         |
|----------------|-----------------------------------------------------------------------------------------------------|
| ID             | UI-02                                                                                               |
| Title          | Create a booking using the UI with valid data                                                       |
| Precondition   | Homepage loaded.                                                                                    |
| Steps          | 1. Fill all required fields in the booking form (name, dates, price, deposit, etc.). <br> 2. Submit form. |
| Expected       | Success message displayed and/or booking appears in UI list. Data matches input.                    |
| Severity       | High                                                                                                |

---

### UI-03 — Booking form — empty required fields

| Field           | Description                                                                    |
|----------------|--------------------------------------------------------------------------------|
| ID             | UI-03                                                                         |
| Title          | Validate required fields cannot be left empty                                 |
| Precondition   | Homepage loaded.                                                               |
| Steps          | 1. Leave one or more mandatory fields empty. <br> 2. Click submit.            |
| Expected       | Inline validation messages; booking is not created; no request with invalid data is sent (if possible to verify). |
| Severity       | High                                                                           |

---

### UI-04 — Booking form — invalid date range

| Field           | Description                                                                 |
|----------------|-----------------------------------------------------------------------------|
| ID             | UI-04                                                                      |
| Title          | Prevent booking when checkout is before checkin                            |
| Precondition   | Homepage loaded.                                                           |
| Steps          | 1. Set checkout date earlier than checkin date. <br> 2. Submit booking.    |
| Expected       | Error/validation message; booking is not created.                          |
| Severity       | High                                                                        |

---

### UI-05 — Booking form — invalid price entry

| Field           | Description                                                     |
|----------------|-----------------------------------------------------------------|
| ID             | UI-05                                                            |
| Title          | Validate price field for non-numeric or negative values         |
| Precondition   | Homepage loaded.                                                |
| Steps          | 1. Input text or negative number into price field. <br> 2. Submit. |
| Expected       | Validation error displayed; booking not created.                 |
| Severity       | Medium                                                          |

---

### UI-06 — View bookings list (if available)

| Field           | Description                                                |
|----------------|------------------------------------------------------------|
| ID             | UI-06                                                     |
| Title          | Display list of existing bookings                          |
| Precondition   | At least one booking exists (created via UI or API).       |
| Steps          | 1. Navigate to bookings list section/page.                 |
| Expected       | Existing bookings displayed with correct core information. |
| Severity       | High                                                       |

---

### UI-07 — Edit booking via UI (happy path)

| Field           | Description                                                                  |
|----------------|------------------------------------------------------------------------------|
| ID             | UI-07                                                                       |
| Title          | Edit an existing booking record through the UI                               |
| Precondition   | At least one visible booking in the UI.                                      |
| Steps          | 1. Click “Edit” for a booking. <br> 2. Change selected fields. <br> 3. Save. |
| Expected       | Success message; updated data displayed.                                     |
| Severity       | High                                                                         |

---

### UI-08 — Edit booking without permission (if auth exists)

| Field           | Description                                                           |
|----------------|-----------------------------------------------------------------------|
| ID             | UI-08                                                                 |
| Title          | Prevent editing bookings without authentication/permissions           |
| Precondition   | User not logged in / has no admin role (if role model exists).       |
| Steps          | 1. Attempt to access edit controls or edit page.                      |
| Expected       | Access denied / redirected to login; no changes applied.             |
| Severity       | High                                                                  |

---

### UI-09 — Delete booking via UI (happy path)

| Field           | Description                                                             |
|----------------|-------------------------------------------------------------------------|
| ID             | UI-09                                                                    |
| Title          | Delete an existing booking from UI                                      |
| Precondition   | At least one booking listed.                                            |
| Steps          | 1. Click “Delete” for a booking. <br> 2. Confirm deletion (if modal).   |
| Expected       | Booking removed from UI list. (Optional: API GET by ID returns `404`.)  |
| Severity       | High                                                                    |

---

### UI-10 — Cancel delete operation

| Field           | Description                                                         |
|----------------|---------------------------------------------------------------------|
| ID             | UI-10                                                                |
| Title          | User can cancel a delete operation                                   |
| Precondition   | Booking present.                                                     |
| Steps          | 1. Click “Delete” for a booking. <br> 2. Click “Cancel” on confirm.  |
| Expected       | Booking remains in list; no delete API call executed.               |
| Severity       | Medium                                                               |

---

### UI-11 — Admin login (if admin area exists)

| Field           | Description                                                   |
|----------------|---------------------------------------------------------------|
| ID             | UI-11                                                         |
| Title          | Log in to admin area with valid credentials                   |
| Precondition   | Admin login page reachable.                                   |
| Steps          | 1. Open admin login. <br> 2. Enter valid credentials. <br> 3. Submit. |
| Expected       | Redirect to admin dashboard with admin controls visible.      |
| Severity       | High                                                          |

---

### UI-12 — Admin login — invalid credentials

| Field           | Description                                                 |
|----------------|-------------------------------------------------------------|
| ID             | UI-12                                                       |
| Title          | Show proper error for invalid admin credentials             |
| Precondition   | Admin login page open.                                      |
| Steps          | 1. Enter invalid username/password. <br> 2. Submit.        |
| Expected       | Error message; user remains on login page; no admin access. |
| Severity       | High                                                        |

---

### UI-13 — Logout and session handling

| Field           | Description                                                     |
|----------------|-----------------------------------------------------------------|
| ID             | UI-13                                                            |
| Title          | Logout should invalidate session                                |
| Precondition   | User logged in to admin area.                                   |
| Steps          | 1. Click “Logout”. <br> 2. Try to open a protected admin URL.   |
| Expected       | Redirected to login; no access to protected content.           |
| Severity       | High                                                            |

---

### UI-14 — Input validation and escaping in UI

| Field           | Description                                                                                               |
|----------------|-----------------------------------------------------------------------------------------------------------|
| ID             | UI-14                                                                                                     |
| Title          | UI handles special characters / scripts safely in user input                                             |
| Precondition   | Homepage or relevant input forms available.                                                              |
| Steps          | 1. Enter `<script>alert(1)</script>` and other special chars into text fields. <br> 2. Submit booking/form. |
| Expected       | Data displayed as plain text; no script executed; no UI breakage.                                        |
| Severity       | High                                                                                                      |

---

### UI-15 — Responsive layout — mobile viewport

| Field           | Description                                                       |
|----------------|-------------------------------------------------------------------|
| ID             | UI-15                                                              |
| Title          | Application is usable in a mobile-like viewport                   |
| Precondition   | Browser/devtools with mobile viewport emulation.                  |
| Steps          | 1. Set viewport ~375x812. <br> 2. Reload homepage and scroll/inspect. |
| Expected       | Main content visible; forms usable without horizontal scrolling.  |
| Severity       | Medium                                                            |

---

### UI-16 — Basic accessibility (labels / keyboard)

| Field           | Description                                                                     |
|----------------|---------------------------------------------------------------------------------|
| ID             | UI-16                                                                          |
| Title          | Basic accessibility support for forms                                          |
| Precondition   | Homepage or form loaded.                                                       |
| Steps          | 1. Check if each input has an associated label. <br> 2. Navigate using Tab key. |
| Expected       | Labels correctly linked; focus order logical; visible focus indicators.        |
| Severity       | Medium                                                                         |

---

### UI-17 — UI behavior when server returns error

| Field           | Description                                                                 |
|----------------|-----------------------------------------------------------------------------|
| ID             | UI-17                                                                      |
| Title          | UI displays friendly error when API fails (e.g., 500)                      |
| Precondition   | Ability to simulate API 5xx (via environment/mocks, if possible).          |
| Steps          | 1. Trigger an API error (create booking while API returns 500).            |
| Expected       | User sees friendly message (not raw JSON/stack trace); can retry or cancel.|
| Severity       | High                                                                        |

---

### UI-18 — Concurrent edits in Admin (sync behavior)

| Field           | Description                                                                                                 |
|----------------|-------------------------------------------------------------------------------------------------------------|
| ID             | UI-18                                                                                                      |
| Title          | Handle concurrent edits of the same booking from multiple admin sessions                                   |
| Precondition   | Booking exists; two admin sessions/windows open on same record.                                            |
| Steps          | 1. In window A, edit and save booking. <br> 2. In window B, with stale data, edit and save.                |
| Expected       | Consistent final state; if conflict detection exists, UI should warn user about outdated data.             |
| Severity       | High                                                                                                       |

---

### UI-19 — Performance with many bookings

| Field           | Description                                                                 |
|----------------|-----------------------------------------------------------------------------|
| ID             | UI-19                                                                      |
| Title          | UI performance when many bookings are present                              |
| Precondition   | Database populated with many bookings (e.g., via API).                     |
| Steps          | 1. Open bookings list page/section.                                        |
| Expected       | Page loads in acceptable time (e.g., < 3s visually); UI remains responsive.|
| Severity       | Medium                                                                      |

---

### UI-20 — Unauthorized actions from UI

| Field           | Description                                                                  |
|----------------|------------------------------------------------------------------------------|
| ID             | UI-20                                                                        |
| Title          | Prevent destructive actions from unauthenticated user                        |
| Precondition   | Not logged in.                                                               |
| Steps          | 1. Try to access admin/edit/delete URLs directly from address bar or UI.    |
| Expected       | Redirect to login / access denied; no create/update/delete executed.        |
| Severity       | High                                                                         |

---

## 3. Integration Test Cases (API ↔ UI)

### INT-01 — Create booking via API and verify in UI

| Field           | Description                                                   |
|----------------|---------------------------------------------------------------|
| ID             | INT-01                                                        |
| Title          | Booking created via API is visible in UI                      |
| Precondition   | API and UI online.                                            |
| Steps          | 1. Create booking with `POST /booking`. <br> 
