# Bug Reports - Restful-Booker Platform

This document contains examples of defects identified during the testing execution, following the documentation standards for high-priority issues.

---

### **BUG-001: Admin Login fails with valid credentials**
*   **Severity:** Critical (Blocker)
*   **Priority:** P1
*   **Status:** Open
*   **Component:** API / Auth
*   **Description:** The system returns a 401 Unauthorized even when providing the correct admin credentials (`admin/password`).
*   **Steps to Reproduce:**
    1. Send a POST request to `/auth/login`.
    2. Use JSON body: `{ "username": "admin", "password": "password" }`.
*   **Expected Result:** Status 200 OK and a session token in the header.
*   **Actual Behavior:** Status 401 Unauthorized with message "Invalid credentials".
*   **Evidence:** [Link to API Log/Screenshot]

---

### **BUG-002: UI allows booking with past dates**
*   **Severity:** High
*   **Priority:** P2
*   **Status:** Open
*   **Component:** UI / Booking Calendar
*   **Description:** The frontend calendar allows a user to select and submit a booking for a date in the past.
*   **Steps to Reproduce:**
    1. Navigate to the homepage.
    2. Click "Book this room".
    3. Select a date range from last month.
    4. Fill in mandatory fields and click "Book".
*   **Expected Result:** The system should prevent past date selection or return a validation error.
*   **Actual Behavior:** The booking is processed, and a "Booking Successful" message is shown.
*   **Evidence:** [Screenshot showing past date selection]

---

### **BUG-003: Contact Form - Missing Email Validation**
*   **Severity:** Medium
*   **Priority:** P3
*   **Status:** Open
*   **Component:** UI / Contact Form
*   **Description:** The contact form accepts invalid email formats (e.g., "test@test" without .com) and submits successfully.
*   **Steps to Reproduce:**
    1. Scroll to the Contact section.
    2. Fill in all fields, but use an invalid email: `user@invalidemail`.
    3. Click "Submit".
*   **Expected Result:** An error message "Must be a well-formed email address" should appear.
*   **Actual Behavior:** The form is submitted without any validation error.
*   **Evidence:** [Screenshot of the submitted form with invalid email]

---

### **BUG-004: Newly created rooms are not visible on the public booking page**
*   **Severity:** High (Major)
*   **Priority:** P1
*   **Status:** Open
*   **Component:** UI / Rooms Synchronization
*   **Description:** 
    Rooms created in the Admin interface are correctly persisted and returned by the `GET /rooms` API, but they are not displayed on the public booking page (`https://automationintesting.online`), even after incognito access and hard refresh. This prevents end users from booking newly created rooms.

*   **Steps to Reproduce:**
    1. Log in to the Admin room management page.
    2. Create a new room with the following example data:
       - Room #: `107`
       - Type: `Single`
       - Accessible: `true`
       - Features: `TV`, `Refreshments`, `Safe`
       - Price: `160`
    3. Confirm that the newly created room (`107`) appears in the Admin room list.
    4. Open browser DevTools, go to **Network → Fetch/XHR**.
    5. Refresh the Admin page or trigger the request that lists rooms and capture the `GET /rooms` call.
    6. Inspect the `GET /rooms` response body and verify that room `107` is present, e.g.:
       ```json
       {
         "rooms": [
           {"roomid":1,"roomName":"101","type":"Single","accessible":true,"image":"/images/room1.jpg","description":"...","features":["TV","WiFi","Safe"],"roomPrice":100},
           {"roomid":2,"roomName":"102","type":"Double","accessible":true,"image":"/images/room2.jpg","description":"...","features":["TV","Radio","Safe"],"roomPrice":150},
           {"roomid":3,"roomName":"103","type":"Suite","accessible":true,"image":"/images/room3.jpg","description":"...","features":["Radio","WiFi","Safe"],"roomPrice":225},
           {"roomid":4,"roomName":"107","type":"Single","accessible":true,"image":"https://www.mwtestconsultancy.co.uk/img/room1.jpg","description":"Please enter a description for this room","features":["TV","Refreshments","Safe"],"roomPrice":160}
         ]
       }
       ```
    7. Open a **new Incognito window**.
    8. Navigate to the public booking page: `https://automationintesting.online`.
    9. Perform a hard refresh (Ctrl + F5).
    10. Check the list of rooms/cards available to the end user on the homepage.

*   **Expected Result:**
    - The newly created room (e.g. room `107`) should be displayed on the public booking page alongside existing rooms (e.g. 101, 102, 103), and be available for selection/booking.
    - The public UI should reflect the data returned by `GET /rooms`.

*   **Actual Behavior:**
    - The `GET /rooms` API response **includes** the newly created room (`roomName: "107"`).
    - The public booking page continues to show only the original rooms (e.g. 101, 102, 103).
    - Room `107` is not visible to the end user, even when using Incognito mode and hard refresh.

*   **Evidence:** 
    - Screenshot of Admin UI showing room `107` in the room list.
    - Screenshot or log of `GET /rooms` response including room `107` as shown above.
    - Screenshot of the public booking page showing only rooms `101`, `102`, `103` and not `107`.
### **BUG-005: Intermittent 418 ("I'm a Teapot") on `GET /booking/{id}` after write operations**
*   **Severity:** High (Major)
*   **Priority:** P1
*   **Status:** Open
*   **Component:** API / Booking Read (`GET /booking/{id}`)
*   **Description:**  
    The `GET /booking/{id}` endpoint intermittently returns HTTP `418` with body `"I'm a Teapot"` immediately after successful write operations (`PUT`, `PATCH`, `DELETE`, or forbidden `DELETE`), even though the same request and `bookingid` return `200` with a valid booking JSON when executed manually via Postman. This behavior introduces flakiness in tests that verify the final state of a booking after updates/deletion and prevents reliable validation of the response schema for `GET /booking/{id}`.

*   **Steps to Reproduce (example for PUT, API-10):**
    1. Authenticate to obtain a valid token:
       ```http
       POST /auth
       Content-Type: application/json

       {
         "username": "admin",
         "password": "password123"
       }
       ```
       - Expected: `200 OK` with `{ "token": "<token>" }`.
    2. Create a new booking:
       ```http
       POST /booking
       Content-Type: application/json

       {
         "firstname": "John",
         "lastname": "Doe",
         "totalprice": 150,
         "depositpaid": true,
         "bookingdates": {
           "checkin": "2026-02-14",
           "checkout": "2026-02-17"
         },
         "additionalneeds": "Breakfast"
       }
       ```
       - Expected: `200`/`201` with:
         ```json
         {
           "bookingid": 899,
           "booking": {
             "firstname": "John",
             "lastname": "Doe",
             "totalprice": 150,
             "depositpaid": true,
             "bookingdates": {
               "checkin": "2026-02-14",
               "checkout": "2026-02-17"
             },
             "additionalneeds": "Breakfast"
           }
         }
         ```
    3. Update the booking via `PUT`:
       ```http
       PUT /booking/899
       Cookie: token=<valid_token>
       Content-Type: application/json

       {
         "firstname": "Gilberto",
         "lastname": "Klein",
         "totalprice": 189,
         "depositpaid": false,
         "bookingdates": {
           "checkin": "2026-02-14",
           "checkout": "2026-02-17"
         },
         "additionalneeds": "Breakfast"
       }
       ```
       - Expected: `200 OK` with the updated booking JSON in the body.
    4. Immediately call:
       ```http
       GET /booking/899
       Accept: application/json
       ```
       - **Sometimes** (e.g., via Postman):  
         Status `200 OK` with the updated JSON:
         ```json
         {
           "firstname": "Gilberto",
           "lastname": "Klein",
           "totalprice": 189,
           "depositpaid": false,
           "bookingdates": {
             "checkin": "2026-02-14",
             "checkout": "2026-02-17"
           },
           "additionalneeds": "Breakfast"
         }
         ```
       - **Frequently in automated tests**:  
         Status `418` with body:
         ```text
         I'm a Teapot
         ```

*   **Other Impacted Scenarios (same 418 pattern on `GET /booking/{id}`):**
    - After forbidden `PUT` without auth (API-11).  
    - After `PATCH` with auth (API-12).  
    - After `DELETE` with auth (API-14) when verifying that the booking no longer exists.  
    - After forbidden `DELETE` without auth (API-15) when verifying that the booking still exists.  
    - After concurrent `PATCH` operations (API-16) when validating the final value of `firstname`.  
    - When validating schema consistency for `GET /booking/{id}` (API-20).

*   **Expected Result:**
    - `GET /booking/{id}` should *consistently* return:
      - `200 OK` with a valid booking JSON when the booking exists; or
      - `404 Not Found` when the booking has been successfully deleted.
    - After successful `PUT`/`PATCH`, `GET /booking/{id}` should reflect the latest state (e.g., “last write wins”).
    - No `418` responses or non-JSON bodies for valid `GET /booking/{id}` operations.

*   **Actual Behavior:**
    - After various write operations, `GET /booking/{id}` intermittently returns:
      - Status: `418`
      - Body: `"I'm a Teapot"`
    - Manual runs via Postman, using the same URL and `bookingid`, sometimes return `200 OK` with valid JSON, indicating inconsistent backend behavior rather than a client/automation issue.
    - This intermittent `418` response breaks:
      - Final state verification after `PUT`/`PATCH`/`DELETE`.
      - Schema validation tests for `GET /booking/{id}`.

*   **Automated Tests Impacted and Mitigation Implemented:**
    - **Update / PUT (API-10):**
      - `BookingUpdateTests.UpdateBooking_Put_WithAuth_OnlyPutResponse` (API-10a): Verifies only the `PUT` response (status, updated body) — **kept green**.  
      - `BookingUpdateTests.UpdateBooking_GetAfterPut_KnownIssue` (API-10b): `GET /booking/{id}` after `PUT` — **marked with `[Skip]`** and documents the 418 issue.
    - **Update / PUT without auth (API-11):**
      - `BookingUpdateTests.UpdateBooking_Put_WithoutAuth_OnlyPutResponse` (API-11a): Verifies that `PUT` without auth is blocked (401/403) — **green**.  
      - `BookingUpdateTests.UpdateBooking_GetAfterForbiddenPut_KnownIssue` (API-11b): `GET /booking/{id}` after forbidden `PUT` — **skipped**, documents 418 issue.
    - **Partial update / PATCH (API-12):**
      - `BookingUpdateTests.PartialUpdateBooking_Patch_WithAuth_OnlyPatchResponse` (API-12a): Verifies only the `PATCH` response — **green**.  
      - `BookingUpdateTests.PartialUpdateBooking_GetAfterPatch_KnownIssue` (API-12b): `GET /booking/{id}` after `PATCH` — **skipped**, documents 418 issue.
    - **Delete with auth (API-14):**
      - `DeleteBookingTests.DeleteBooking_WithAuth_RemovesBooking`: `DELETE` + subsequent `GET` expected `404`. When `GET` returns 418, the test fails with a clear “Known issue” message and is used as evidence of the bug.
    - **Delete without auth (API-15):**
      - `DeleteBookingTests.DeleteBooking_WithoutAuth_IsRejected`: Validates only that `DELETE` without auth is blocked (401/403) — **green**.  
      - `DeleteBookingTests.DeleteBooking_GetAfterForbiddenDelete_KnownIssue` (15b, if enabled): `GET` after forbidden `DELETE` — designed to be **skipped** or fail with a “Known issue” when 418 occurs.
    - **Concurrent updates (API-16):**
      - `AdvancedBookingTests.ConcurrentUpdates_Patch_OnlyPatchResponses` (API-16a): Verifies that both concurrent `PATCH` calls return 200 — **green**.  
      - `AdvancedBookingTests.ConcurrentUpdates_LastWriteWins_KnownIssue` (API-16b): Uses `GET /booking/{id}` to validate final state — **skipped**, documents 418 issue.
    - **Schema validation (API-20):**
      - `AdvancedBookingTests.ResponseSchemas_BookingListAndAuth_AreConsistent` (API-20a): Validates schema for `/booking` (list) and `/auth` — **green**.  
      - `AdvancedBookingTests.ResponseSchema_BookingById_KnownIssue` (API-20b): Validates contract of `GET /booking/{id}` — **skipped**, documents the 418 behavior.

*   **Solution Applied in Test Suite:**
    - Split tests into A/B variants:
      - **A-tests** (e.g., API-10a, 11a, 12a, 16a, 20a): focus on the primary behavior (PUT/PATCH/DELETE success, auth, schema for `/booking` list and `/auth`) and remain **green**.
      - **B-tests** (e.g., API-10b, 11b, 12b, 16b, 20b, and additional GET-after-delete scenarios): specifically exercise `GET /booking/{id}` after writes and are **marked with `[Skip]`** along with explicit “Known issue: 418…” messages.
    - Implemented a lightweight retry helper (`BookingTestHelper.GetBookingByIdWithRetryAsync`) to mitigate transient issues. Even with retry, the `418` persists, reinforcing that this is a backend behavior, not a timing issue in the tests.
    - Enhanced failure messages and logs (`ITestOutputHelper`) in impacted tests to capture:
      - Created `bookingid`,
      - Exact URL of `GET /booking/{id}`,
      - Final status and body (`"I'm a Teapot"`),
      - Divergence from Postman behavior.

*   **Evidence:**
    - Test logs (examples):
      - `BookingUpdateTests.UpdateBooking_Put_WithAuth_UpdatesAllFields`  
        - `[CREATE] booking id: 899`  
        - `[PUT] Status: 200 - OK` + updated JSON  
        - `[GET after PUT] Status: 418 - 418`  
        - `[GET after PUT] Body  : I'm a Teapot`
      - `AdvancedBookingTests.ResponseSchema_BookingById_KnownIssue`  
        - `[CREATE] booking id: 899`  
        - `GET /booking/899` → `418 "I'm a Teapot"` when validating schema.
    - Postman runs:
      - Same `bookingid` and URL (`GET https://restful-booker.herokuapp.com/booking/899`) sometimes return `200 OK` with a valid JSON, confirming inconsistent server behavior for the same resource and route.
