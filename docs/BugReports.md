# Bug Reports - Restful-Booker Platform

This document contains examples of defects identified during the testing execution, following the documentation standards for high-priority issues.

---

### **BUG-001: Newly created rooms are not visible on the public booking page**
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

---

### **BUG-002: Booking conflict and invalid date range crash the SPA instead of showing a friendly error**

*   **Severity:** High (Major)
*   **Priority:** P1
*   **Status:** Open
*   **Component:** UI / Booking Flow
*   **Description:**
    When the API returns a valid error for booking conflicts or invalid date ranges (e.g., check‑in later than check‑out), the frontend SPA (`https://automationintesting.online`) does not display a user-friendly validation message. Instead, it displays a generic “Application error” banner, indicating a client-side exception.

*   **Steps to Reproduce (Conflict Scenario):**
    1. Create a booking for a given room and date range (via UI or API).
    2. Attempt to create a **second** booking for the **same room and same dates** via the UI.
    3. Observe the behavior after submitting the booking form.

*   **Steps to Reproduce (Invalid Date Scenario):**
    1. Open the homepage: `https://automationintesting.online`.
    2. Open the booking form for any room.
    3. Set a **check‑in date that is later than the check‑out date**.
    4. Submit the booking.

*   **Expected Result:**
    - A clear validation message is shown in the UI, such as:
      - “This room is already booked for the selected dates.” or
      - “Check‑out date must be after check‑in date.”
    - No JavaScript exceptions or generic “Application error” pages.
    - The user can correct the input and try again without reloading the entire app.

*   **Actual Behavior:**
    - Backend correctly returns an error (e.g., `409 Conflict` with body `{"error":"Failed to create booking"}` for conflicts).
    - The SPA displays a generic message:
      - `Application error: a client-side exception has occurred while loading automationintesting.online (see the browser console for more information).`
    - The UI effectively “crashes” for this flow, and the user does not see why the booking failed.

*   **Evidence:**
    - Network tab showing the `POST /booking` call returning `409 Conflict` with `{"error":"Failed to create booking"}` or 4xx for invalid dates.
    - Screenshot of the UI showing the “Application error” banner after submitting the form.
    - Browser console logs showing an unhandled JavaScript error when processing the 4xx/409 response.

---

### **BUG-003: Content-Type enforcement for POST /booking is too permissive**

*   **Severity:** Medium
*   **Priority:** P2
*   **Status:** Open
*   **Component:** API / Booking Endpoint
*   **Description:**
    The `POST /booking` endpoint accepts requests with incorrect or missing `Content-Type` headers (e.g., `text/plain` or no `Content-Type` at all) and sometimes proceeds to create a booking, instead of rejecting the request with a 4xx error.

*   **Steps to Reproduce:**
    1. Send a `POST /booking` request with a valid JSON body, but:
       - `Content-Type: text/plain`, or
       - No `Content-Type` header at all.
    2. Inspect the HTTP status code and response body.
    3. Optionally, try to GET or DELETE the created booking to confirm it exists.

*   **Expected Result:**
    - The API enforces the contract and rejects such requests with a `4xx` status (e.g., `400 Bad Request` or `415 Unsupported Media Type`).
    - No booking is created when `Content-Type` is incorrect or missing.

*   **Actual Behavior:**
    - In some cases the API returns `200/201` and creates a booking even when:
      - `Content-Type` is `text/plain`, or
      - The header is missing entirely.
    - The response body includes a valid `bookingid` and booking data, indicating the resource was persisted.

*   **Evidence:**
    - Logs from automated tests showing `POST /booking` with:
      - `Content-Type: text/plain`, or
      - No `Content-Type`, 
      returning `201 Created` with a JSON body containing `bookingid`.
    - Subsequent `GET /booking/{id}` showing the booking exists.
    - The test `API-19 - Reject POST /booking with wrong Content-Type` documents and reproduces this behavior (including cleanup).

---

### **BUG-004: Update Room without valid Auth returns 500 with generic “unexpected error”**

*   **Severity:** High (Major)
*   **Priority:** P1
*   **Status:** Open
*   **Component:** API / Room Endpoint (Authorization)
*   **Description:**
    When attempting to update a room via `PUT /api/room/{roomid}` **without** valid authentication, the API responds with `500 Internal Server Error` and a generic `"An unexpected error occurred"` message, instead of a clear `401/403` authorization error.

*   **Steps to Reproduce:**
    1. Create a room via:
       - `POST /api/room/` with valid auth, or 
       - Use an existing room ID from the demo data.
    2. Send a `PUT /api/room/{roomid}` request with a valid JSON body **without** including a valid auth token/cookie.
    3. Inspect the response status and body.

*   **Expected Result:**
    - The API should return a `4xx` status indicating an authorization problem (e.g., `401 Unauthorized` or `403 Forbidden`).
    - The room should not be updated.
    - Error message should clearly state that valid authentication/authorization is required.

*   **Actual Behavior:**
    - The API returns `500 Internal Server Error` with a body similar to:
      ```json
      {
        "errors": ["An unexpected error occurred"]
      }
      ```
    - This hides the real cause (missing/invalid auth) and suggests a server crash rather than an authorization failure.

*   **Evidence:**
    - Automated test `API-27 - Update Room without valid Auth` producing `500` responses with `"errors": ["An unexpected error occurred"]`.
    - Request/response logs showing `PUT /api/room/{roomid}` without auth resulting in status `500`.
    - Confirmation that the request body is valid JSON and that the same body works when sent with a valid token (i.e., issue is specific to missing auth handling, not payload validation).
