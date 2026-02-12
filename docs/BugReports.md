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
