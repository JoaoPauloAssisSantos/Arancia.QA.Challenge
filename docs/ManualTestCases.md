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

### API-21 — Create a room with valid data

| Field           | Description                                                                                  |
|----------------|----------------------------------------------------------------------------------------------|
| ID             | API-21                                                                                       |
| Title          | Create a room with valid data                                                                                             |
| Precondition   | get token /api/auth/login                                                                                                                        |
| Steps          | 1. Send `POST /api/room/` with a fully valid JSON body (all required fields).                                                  |
| Example body   | `{"roomName": "700","type": "Suite","accessible": true,"image": "https://blog.postman.com/wp-content/uploads/2014/07/logo.png","description": "This is room 101, dare you enter?","roomPrice": 100,"features": ["WiFi", "Safe"]}` |
| Expected       | Status `200` and room created                            |
| Severity       | Medium                                                                                           |

---

### API-22 — Create a room without valid data

| Field           | Description                                                                                  |
|----------------|----------------------------------------------------------------------------------------------|
| ID             | API-22                                                                                     |
| Title          | Create a room without valid data                                                                                             |
| Precondition   | get token /api/auth/login                                                                                                                        |
| Steps          | 1. Send `POST /api/room/` without valid JSON body.                                                  |
| Example body   | `{"roomName": "","type": "","accessible": ,"image": "https://blog.postman.com/wp-content/uploads/2014/07/logo.png","description": "This is room 101, dare you enter?","roomPrice": 100,"features": ["WiFi", "Safe"]}` |
| Expected       | Status 400/500.                           |
| Severity       | High                                                                                           |

---

### API-23 — Create a room with valid data and Get Room

| Field           | Description                                                                                  |
|----------------|----------------------------------------------------------------------------------------------|
| ID             | API-23                                                                                  |
| Title          | Create a room with valid data and Get Room by ID                                                                                            |
| Precondition   | get token /api/auth/login                                                                                                                        |
| Steps          | 1. Send `POST /api/room/` with a fully valid JSON body (all required fields). 2. Send 'GET /api/room/{roomid}'                                       |
| Expected       | Get response with created room information.                  |
| Severity       | High                                                                                           |

---

### API-24 — Create Room Without valid Auth

| Field           | Description                                                                                  |
|----------------|----------------------------------------------------------------------------------------------|
| ID             | API-24                                                                                  |
| Title          | Create a room without valid Auth                                                                                            |
| Precondition   | None                                                                                                                        |
| Steps          | 1. Send `POST /api/room/` without valid Auth                                       |
| Expected       | Status `403`/`401`. Room not created                 |
| Severity       | High                                                                                           |

---

### API-25 — Get Room Without valid RoomId

| Field           | Description                                                                                  |
|----------------|----------------------------------------------------------------------------------------------|
| ID             | API-25                                                                                 |
| Title          | Get a room with valid Auth                                                                                            |
| Precondition   | Create a room `POST /api/room/`                                                                                                                     |
| Steps          | 1. Send `GET /api/room/{roomid}`                                   |
| Expected       | Status `400`. `{"timestamp": "2026-02-15T20:27:26.541+00:00","status": 400,"error": "Bad Request","path": "/room/9849494984"}`               |
| Severity       | High                                                                                           |

---

### API-26 — Update Room with valid Auth

| Field           | Description                                                                                  |
|----------------|----------------------------------------------------------------------------------------------|
| ID             | API-26                                                                                |
| Title          | Update a room with valid Auth                                                                                            |
| Precondition   | Create a room `POST /api/room/`                                                                                                                     |
| Steps          | 1. Send `PUT /api/room/{roomid}`                                   |
| Expected       | Status `200`. A subsequent `GET /booking/{id}` returns the new values.                  |
| Severity       | High                                                                                           |

---

### API-27 — Update Room without valid Auth

| Field           | Description                                                                                  |
|----------------|----------------------------------------------------------------------------------------------|
| ID             | API-27                                                                               |
| Title          | Update a room without valid Auth                                                                                            |
| Precondition   | Create a room `POST /api/room/`                                                                                                                     |
| Steps          | 1. Send `PUT /api/room/{roomid}`                                   |
| Expected       | Status `500`. Room does not updates  `{"errors": ["An unexpected error occurred"]} `            |
| Severity       | High                                                                                           |

---

### API-28 — Update Room without valid data

| Field           | Description                                                                                  |
|----------------|----------------------------------------------------------------------------------------------|
| ID             | API-28                                                                               |
| Title          | Update a room without valid Data                                                                                            |
| Precondition   | Create a room `POST /api/room/`                                                                                                                     |
| Steps          | 1. Send `PUT /api/room/{roomid}` 
| Example body   | `{"roomName": "","type": "","accessible": 0 ,"image": "https://blog.postman.com/wp-content/uploads/2014/07/logo.png","description": "This is room 101, dare you enter?","roomPrice": 8000000,"features": ["WiFi", "Safe"]}`                                  |
| Expected       | Status `400`. Room not updated . response with `"errors" : [{errors}]`              |
| Severity       | High                                                                                           |

---

### API-29 — Delete Room with valid Auth

| Field           | Description                                                                                  |
|----------------|----------------------------------------------------------------------------------------------|
| ID             | API-29                                                                              |
| Title          | Delete a room with valid Auth                                                                                            |
| Precondition   | Create a room `POST /api/room/`                                                                                                                     |
| Steps          | 1. Send `DELETE /api/room/{roomid}`                |
| Expected       | Status `200`  and room deleted `GET /api/room/{DELETEDroomid}` does not return the room       |
| Severity       | High                                                                                           |

---

### API-30 — Delete Room without valid Auth

| Field           | Description                                                                                  |
|----------------|----------------------------------------------------------------------------------------------|
| ID             | API-30                                                                               |
| Title          | Delete a room without valid Auth                                                                                            |
| Precondition   | Create a room `POST /api/room/`                                                                                                                     |
| Steps          | 1. Send `DELETE /api/room/{roomid}`                |
| Expected       | Status `400` room does not delete      |
| Severity       | High                                                                                           |

---

### API-31 — Create room and create a booking

| Field           | Description                                                                                  |
|----------------|----------------------------------------------------------------------------------------------|
| ID             | API-31                                                                               |
| Title          | Booking for a created room                                                                                           |
| Precondition   | Create a room `POST /api/room/`                                                                                                                     |
| Steps          | 1. Send `POST /api/booking`                |
| Expected       | Status `200` booking created for the new room      |
| Severity       | High                                                                                           |

---

### API-32 — Create room Update room and create a booking

| Field           | Description                                                                                  |
|----------------|----------------------------------------------------------------------------------------------|
| ID             | API-32                                                                               |
| Title          | Booking for a created and updated room                                                                                           |
| Precondition   | Create a room `POST /api/room/`                                                                                                                     |
| Steps          | 1. Send `POST /api/booking`                |
| Expected       | Status `200` booking created for the new room      |
| Severity       | High                                                                                           |

---

### API-33 — Creating booking for room already booked on same dates returns 409

| Field           | Description                                                                                  |
|----------------|----------------------------------------------------------------------------------------------|
| ID             | API-33                                                                               |
| Title          | Booking for a created and updated room                                                                                           |
| Precondition   | Create a room `POST /api/room/`                                                                                                                     |
| Steps          | 1. Send `POST /api/booking`                |
| Expected       | Status `409` Conflict `{"error": "Failed to create booking"}`      |
| Severity       | High                                                                                           |

---

### API-34 — Creating room and get rooms

| Field           | Description                                                                                  |
|----------------|----------------------------------------------------------------------------------------------|
| ID             | API-33                                                                               |
| Title          | Creating room and get rooms                                                                                          |
| Precondition   | Create a room `POST /api/room/`                                                                                                                     |
| Steps          | 1. Send `POST /api/booking`  2. `GET /api/room/`              |
| Expected       | Status `200` `{"rooms": [list of room with new room added]}`      |
| Severity       | Medium                                                                                           |

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
| Steps          | 1. Fill all required fields in the booking form (firstname, lastname, email, phone). <br> 2. Submit form. |
| Expected       | Success message displayed and/or booking appears in UI list. Data matches input.                    |
| Severity       | High                                                                                                |

---

### UI-03 — Booking form — empty required fields

| Field           | Description                                                                    |
|----------------|--------------------------------------------------------------------------------|
| ID             | UI-03                                                                         |
| Title          | Validate required fields cannot be left empty                                 |
| Precondition   | Homepage loaded.                                                               |
| Steps          | 1. Fill with wrong information required fields in the booking form (firstname, lastname, email, phone). <br> 2. Submit form. |
| Expected       | Error message for each field.                    |
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

### UI-05 — Submit booking for a date and room that is already booked

| Field           | Description                                                     |
|----------------|-----------------------------------------------------------------|
| ID             | UI-05                                                            |
| Title          | Submit booking for a date and room that is already booked         |
| Precondition   | Homepage loaded.                                                |
| Steps          | 1. Submit a booking for a room <br> 2. Submit a booking for the same room and date booked before. |
| Expected       | Validation error displayed; booking not created.                 |
| Severity       | Medium                                                          |

---


### UI-06 — Admin login

| Field           | Description                                                   |
|----------------|---------------------------------------------------------------|
| ID             | UI-06                                                         |
| Title          | Log in to admin area with valid credentials                   |
| Precondition   | Admin login page reachable.                                   |
| Steps          | 1. Open admin login. <br> 2. Enter valid credentials. <br> 3. Submit. |
| Expected       | Redirect to admin dashboard with admin controls visible.      |
| Severity       | High                                                          |

---

### UI-07 — Admin login — invalid credentials

| Field           | Description                                                 |
|----------------|-------------------------------------------------------------|
| ID             | UI-07                                                       |
| Title          | Show proper error for invalid admin credentials             |
| Precondition   | Admin login page open.                                      |
| Steps          | 1. Enter invalid username/password. <br> 2. Submit.        |
| Expected       | Error message: Invalid credentials; user remains on login page; no admin access. |
| Severity       | High                                                        |

---

### UI-08 — Logout and session handling

| Field           | Description                                                     |
|----------------|-----------------------------------------------------------------|
| ID             | UI-08                                                            |
| Title          | Logout should invalidate session                                |
| Precondition   | User logged in to admin area.                                   |
| Steps          | 1. Click “Logout”. <br> 2. Try to open a protected admin URL.   |
| Expected       | Redirected to login; no access to protected content.           |
| Severity       | High                                                            |

---

### UI-09 — Input validation and escaping in UI

| Field           | Description                                                                                               |
|----------------|-----------------------------------------------------------------------------------------------------------|
| ID             | UI-09                                                                                                  |
| Title          | UI handles special characters / scripts safely in user input                                             |
| Precondition   | Homepage or relevant input forms available.                                                              |
| Steps          | 1. Enter `<script>alert(1)</script>` and other special chars into text fields. <br> 2. Submit booking/form. |
| Expected       | Data displayed as plain text; no script executed; no UI breakage.                                        |
| Severity       | High                                                                                                      |

---

### UI-10 — Unauthorized actions from UI

| Field           | Description                                                                  |
|----------------|------------------------------------------------------------------------------|
| ID             | UI-10                                                                        |
| Title          | Prevent destructive actions from unauthenticated user                        |
| Precondition   | Not logged in.                                                               |
| Steps          | 1. Try to access admin/edit/delete URLs directly from address bar or UI.    |
| Expected       | Redirect to login / access denied; no create/update/delete executed.        |
| Severity       | High                                                                         |

---

### UI-11 — Create booking via API and verify in UI

| Field           | Description                                                   |
|----------------|---------------------------------------------------------------|
| ID             | INT-21                                                        |
| Title          | Booking created via API is visible in UI                      |
| Precondition   | API and UI online.                                            |
| Steps          | 1. Create booking with `POST /booking`. <br> 

---

### UI-12 — Admin creates room via UI and it appears in UI and API

| Field           | Description                                                   |
|----------------|---------------------------------------------------------------|
| ID             | INT-12                                                      |
| Title          | Booking created via UI is visible in UI and API Get                      |
| Precondition   | API and UI online.                                            |
| Steps          | 1. Create booking with url https://automationintesting.online/admin/rooms. <br> 

---

### UI-13 — Admin creates room via POST API and it appears in UI and API

| Field           | Description                                                   |
|----------------|---------------------------------------------------------------|
| ID             | INT-13                                                      |
| Title          | Booking created via API is visible in UI and API Get                      |
| Precondition   | API and UI online.                                            |
| Steps          | 1. Create booking with `POST /booking`. <br> 

---

### UI-14 — Performance with many bookings

| Field           | Description                                                                 |
|----------------|-----------------------------------------------------------------------------|
| ID             | UI-14                                                                      |
| Title          | UI performance when many bookings are present                              |
| Precondition   | Database populated with many bookings (e.g., via API).                     |
| Steps          | 1. Open bookings list page/section.                                        |
| Expected       | Page loads in acceptable time (e.g., < 3s visually); UI remains responsive.|
| Severity       | Medium                                                                      |

---
