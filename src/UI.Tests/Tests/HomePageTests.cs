using Arancia.Test.API.Clients;
using Arancia.Test.API.Helpers;
using FluentAssertions;
using OpenQA.Selenium;
using System.Net;
using System.Text.Json;
using UI.Tests.Fixture;
using UI.Tests.Helpers;
using Xunit.Abstractions;

public class HomePageTests : IClassFixture<WebDriverFixture>
{
    private readonly WebDriverFixture _fix;
    private readonly HomePage _home;
    private readonly RoomPage _room;
    private readonly ITestOutputHelper _output;
    public HomePageTests(WebDriverFixture fix, ITestOutputHelper output)
    {
        _fix = fix ?? throw new ArgumentNullException(nameof(fix));
        _home = new HomePage(_fix.Driver, "https://automationintesting.online");
        _room = new RoomPage(_fix.Driver, "https://automationintesting.online");
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    [Fact(DisplayName = "UI-01 - Homepage loads successfully")]
    public void Homepage_LoadsSuccessfully()
    {
        _home.IsLoaded().Should().BeTrue();
    }

    [Fact(DisplayName = "UI-02 - Creating Booking and show confirmation")]
    public void CreateBooking()
    {
        // Arrange
        _home.GoTo();
        _home.IsLoaded().Should().BeTrue();
        _home.ClickBookNow();
        var(checkin, checkout) = _home.SetBookingDates();
        // ensure we target the correct room card (e.g., "Suite")
        _home.ClickBookNowForRoom("Suite");

        // Act
        // Click the in-card "Book now" then the reservation button and fill form
        _room.ClickReserveNow();
        var contact = _room.FillContactFormAndReturnData(); // returns Contact with checkin/checkout maybe not set
                                                           // If there is a separate date picker step, ensure dates are set before submit here.
        _room.ClickReserveNowForBooking(); // or SubmitBooking equivalent

        // Wait for confirmation UI to appear
        var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(_fix.Driver, TimeSpan.FromSeconds(10));
        var confirmationHeading = wait.Until(d =>
        {
            try
            {
                var el = d.FindElement(OpenQA.Selenium.By.XPath("//h2[normalize-space() = 'Booking Confirmed']"));
                return (el.Displayed) ? el : null;
            }
            catch { return null; }
        });

        // Assert
        confirmationHeading.Should().NotBeNull("Booking Confirmed heading should be visible");

        // Assert the confirmation paragraph text exists and starts with the expected sentence
        var confirmationParagraph = _fix.Driver.FindElement(OpenQA.Selenium.By.XPath("//h2[normalize-space() = 'Booking Confirmed']/following-sibling::p"));
        confirmationParagraph.Text.Should().Contain("Your booking has been confirmed for the following dates:");
    }

    [Theory(DisplayName = "UI-03 - Booking form — empty or invalid required fields")]
    [InlineData("emptyFirst", "", "ValidLast", "email@example.com", "12345678901")]
    [InlineData("shortFirst", "Jo", "ValidLast", "email@example.com", "12345678901")]
    [InlineData("longFirst", "FirstNameOverEighteenChars!", "ValidLast", "email@example.com", "12345678901")]
    [InlineData("emptyLast", "ValidFirst", "", "email@example.com", "12345678901")]
    [InlineData("shortLast", "ValidFirst", "Li", "email@example.com", "12345678901")]
    [InlineData("longLast", "ValidFirst", "VeryLongLastNameOverThirtyCharactersHere", "email@example.com", "12345678901")]
    [InlineData("phoneShort", "ValidFirst", "ValidLast", "email@example.com", "12345")]
    [InlineData("phoneLong", "ValidFirst", "ValidLast", "email@example.com", "12345678901234567890123456789012345")]
    public void ContactValidation_ShowsError_ForInvalidInputs(string caseId, string first, string last, string email, string phone)
    {
        // Arrange
        _home.GoTo();
        _home.IsLoaded().Should().BeTrue();
        _home.ClickBookNow();
        var (checkin, checkout) = _home.SetBookingDates();

        // open booking UI for specific card
        _home.ClickBookNowForRoom("Suite"); // adjust room name if needed
        _room.ClickReserveNow();

        // prepare contact with provided invalid values
        var contact = new Contact
        {
            FirstName = first,
            LastName = last,
            Email = email,
            Phone = phone
        };

        // Act - fill form and submit
        _room.FillContactForm(contact);
        _room.ClickReserveNowForBooking(); // submit booking

        // Assert - validation messages should appear
        var messages =_room.GetVisibleValidationMessages();
        messages.Should().NotBeEmpty($"expected validation messages for case '{caseId}'");

        // optional: log messages for debugging
        foreach (var m in messages) _fix.Driver.SwitchTo().DefaultContent(); // no-op to quiet analyzers
    }
    [Fact(DisplayName = "UI-04 - Booking form — invalid date range")]
    public void Booking_CheckinAfterCheckout_ShowsFailedToCreateBooking_OrCaptureArtifacts()
    {
        // Arrange
        _home.GoTo();
        _home.IsLoaded().Should().BeTrue();
        _home.ClickBookNow();

        // Act / Prepare invalid dates: checkin after checkout
        var checkin = DateTime.UtcNow.Date.AddDays(10);
        var checkout = checkin.AddDays(-2); // invalid: checkout before checkin
                                            // Use RoomPage method to set the dates; returns applied values
        var (appliedCheckin, appliedCheckout) = _home.SetBookingDates(checkin, checkout);


        _home.ClickBookNowForRoom("Suite");
        _room.ClickReserveNow();
        // Fill contact with (valid) random data
        var contact = _room.FillContactFormAndReturnData(); // generates and fills random contact

        // Submit reservation (adjust method name if different)
        _room.ClickReserveNowForBooking(); // or room.ClickReserveNowForBooking()

        // Assert - look for expected error message on UI
        try
        {
            var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(_fix.Driver, TimeSpan.FromSeconds(6));

            // Wait for either explicit inline error or application error text
            var errorEl = wait.Until(d =>
            {
                try
                {
                    // common error containers
                    var candidate = d.FindElements(OpenQA.Selenium.By.CssSelector(".alert-danger, .text-danger, .form-error, .invalid-feedback"))
                                     .FirstOrDefault(e => e.Displayed && e.Text.Trim().Length > 0);
                    if (candidate != null) return candidate;

                    // application error page headline
                    var appErr = d.FindElements(OpenQA.Selenium.By.XPath("//*[contains(text(),'Application error') or contains(text(),'Failed to create booking')]"))
                                  .FirstOrDefault(e => e.Displayed);
                    if (appErr != null) return appErr;

                    return null;
                }
                catch { return null; }
            });

            // If we have an element, assert it contains the expected message
            errorEl.Should().NotBeNull(because: "the UI should show an error message when booking fails");
            var msg = errorEl.Text.Trim();
            msg.Should().Contain("Failed to create booking", because: "the backend returns 409 with error and UI should reflect it");
        }
        catch (Exception ex)
        {
            // On failure, capture artifacts for bug triage (screenshot + console logs) and fail with details.
            try
            {
                // Screenshot
                var ss = ((ITakesScreenshot)_fix.Driver).GetScreenshot();
                var screenshotFile = $"booking_invalid_dates_failed_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png";
                System.IO.File.WriteAllBytes(screenshotFile, ss.AsByteArray);

                // Console logs (browser)
                var consoleLogs = new List<string>();
                try
                {
                    var logEntries = _fix.Driver.Manage().Logs.GetLog(OpenQA.Selenium.LogType.Browser);
                    foreach (var entry in logEntries) consoleLogs.Add($"{entry.Timestamp} {entry.Level} {entry.Message}");
                }
                catch (Exception) { consoleLogs.Add("Could not collect browser console logs (not supported)."); }

                // Page source / current URL
                var currentUrl = _fix.Driver.Url;
                var pageSourceFile = $"booking_invalid_dates_page_{DateTime.UtcNow:yyyyMMdd_HHmmss}.html";
                System.IO.File.WriteAllText(pageSourceFile, _fix.Driver.PageSource);

                // Compose failure message
                var details = new System.Text.StringBuilder();
                details.AppendLine("Booking test failed to find expected error on UI.");
                details.AppendLine($"Exception: {ex.Message}");
                details.AppendLine($"URL: {currentUrl}");
                details.AppendLine($"Screenshot: {screenshotFile}");
                details.AppendLine($"Page source saved to: {pageSourceFile}");
                details.AppendLine("Console logs:");
                details.AppendLine(string.Join(Environment.NewLine, consoleLogs));

                // Optionally write to test output (if you have ITestOutputHelper available)
                try { _output?.WriteLine(details.ToString()); } catch { /* ignore */ }

                // Fail the test with the composed message
                throw new Xunit.Sdk.XunitException(details.ToString());
            }
            catch (Exception finalEx)
            {
                throw new Xunit.Sdk.XunitException("Failed to assert booking error and failed to capture artifacts: " + finalEx.Message);
            }
        }
    }

    [Fact(DisplayName = "UI-05 - Submit booking for a date and room that is already booked (API pre-create)")]
    public async Task SubmitBooking_ForAlreadyBookedRoom_UsesApiPrecreate_ThenUiAttempt()
    {
        // Arrange
        _home.GoTo();
        _home.IsLoaded().Should().BeTrue();
        _home.ClickBookNow();
        // Use admin token to find room id for "Suite" via API
        var adminAuth = new AutomationTestingAuthClient();
        var adminToken = await adminAuth.GetTokenAsync("admin", "password");
        adminToken.Should().NotBeNullOrWhiteSpace();

        var roomClient = new RoomClient(ApiClientFactory.Create(Arancia.Test.API.Helpers.Settings.AutomationTestingApiBase));
        var roomsResp = await roomClient.GetRoomsAsync(adminToken);
        BookingTestHelper.LogRequestResponse(_output, "GET /room (for lookup)", roomsResp);
        roomsResp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var roomsDoc = JsonDocument.Parse(roomsResp.Content!);
        roomsDoc.RootElement.TryGetProperty("rooms", out var roomsEl).Should().BeTrue();

        int roomId = -1;
        var targetRoomName = "103";
        var availableRoomNames = new List<string>();

        foreach (var r in roomsEl.EnumerateArray())
        {
            if (!r.TryGetProperty("roomName", out var rnEl))
                continue;

            var name = rnEl.GetString() ?? string.Empty;
            availableRoomNames.Add(name);

            if (string.Equals(name.Trim(), targetRoomName, StringComparison.OrdinalIgnoreCase) &&
                r.TryGetProperty("roomid", out var rid) && rid.ValueKind == JsonValueKind.Number)
            {
                roomId = rid.GetInt32();
                break;
            }
        }

        // helpful log if not found
        if (roomId <= 0)
        {
            _output.WriteLine($"Target roomName '{targetRoomName}' not found. Available roomNames: {string.Join(", ", availableRoomNames)}");
        }

        roomId.Should().BeGreaterThan(0, $"Room with roomName '{targetRoomName}' must exist in rooms list");

        // Choose conflicting dates
        var checkin = DateTime.UtcNow.Date.AddDays(7);
        var checkout = checkin.AddDays(2);

        // Create first booking via API (precondition)
        var bookingClient = new BookingClient(ApiClientFactory.Create(Arancia.Test.API.Helpers.Settings.AutomationTestingApiBase));
        var firstBooking = BookingFactory.CreateRandomBooking();
        firstBooking.roomid = roomId;
        firstBooking.bookingdates = new BookingDates { checkin = checkin.ToString("yyyy-MM-dd"), checkout = checkout.ToString("yyyy-MM-dd") };

        var apiCreateResp = await bookingClient.CreateBookingAsync(firstBooking);
        BookingTestHelper.LogRequestResponse(_output, "POST /booking (api precreate)", apiCreateResp);
        ((int)apiCreateResp.StatusCode).Should().BeOneOf(200, 201);

        using var apiDoc = JsonDocument.Parse(apiCreateResp.Content!);
        apiDoc.RootElement.TryGetProperty("bookingid", out var apiBookingIdEl).Should().BeTrue();
        var apiBookingId = apiBookingIdEl.GetInt32();

        // Set the same dates in UI (use your RoomPage method to set dates)
        _home.SetBookingDates(checkin, checkout);
        // Act: attempt second booking via UI for same room and same dates
        _home.GoTo();
        _home.ClickBookNowForRoom("Suite");
        _room.ClickReserveNow();

        // Fill contact and submit
        var contact = _room.FillContactFormAndReturnData();
        _room.ClickReserveNowForBooking(); // submit via UI

        // Assert: expect UI to show "Failed to create booking" or application error
        try
        {
            var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(_fix.Driver, TimeSpan.FromSeconds(8));
            var errEl = wait.Until(d =>
            {
                try
                {
                    var e = d.FindElements(By.CssSelector(".alert-danger, .text-danger, .form-error, .booking-error"))
                             .FirstOrDefault(x => x.Displayed && x.Text.IndexOf("Failed to create booking", StringComparison.OrdinalIgnoreCase) >= 0);
                    if (e != null) return e;
                    var app = d.FindElements(By.XPath("//*[contains(text(),'Application error') or contains(text(),'Failed to create booking')]"))
                               .FirstOrDefault(x => x.Displayed);
                    return app;
                }
                catch { return null; }
            });

            errEl.Should().NotBeNull("Expected UI to show a booking failure message");
            errEl.Text.IndexOf("Failed to create booking", StringComparison.OrdinalIgnoreCase)
    .Should().BeGreaterThanOrEqualTo(0, "expected error message to contain 'Failed to create booking' (case-insensitive)");
        }
        catch (Exception ex)
        {
            // capture artifacts for triage
            var ts = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            try
            {
                var ss = ((ITakesScreenshot)_fix.Driver).GetScreenshot();
                var screenshotFile = $"booking_conflict_{ts}.png";
                System.IO.File.WriteAllBytes(screenshotFile, ss.AsByteArray);
                _output.WriteLine($"Saved screenshot: {screenshotFile}");
            }
            catch (Exception se) { _output.WriteLine($"Screenshot failed: {se.Message}"); }

            try
            {
                var pageFile = $"booking_conflict_{ts}.html";
                System.IO.File.WriteAllText(pageFile, _fix.Driver.PageSource);
                _output.WriteLine($"Saved page source: {pageFile}");
            }
            catch (Exception pe) { _output.WriteLine($"Save page source failed: {pe.Message}"); }

            try
            {
                var logs = new List<string>();
                var entries = _fix.Driver.Manage().Logs.GetLog(LogType.Browser);
                logs.AddRange(entries.Select(e => $"{e.Timestamp} {e.Level} {e.Message}"));
                var logFile = $"booking_conflict_console_{ts}.log";
                System.IO.File.WriteAllLines(logFile, logs);
                _output.WriteLine($"Saved console logs: {logFile}");
            }
            catch (Exception le) { _output.WriteLine($"Collect console logs failed: {le.Message}"); }

            // cleanup the api-created booking (best-effort) and fail
            try { await bookingClient.DeleteBookingAsync(apiBookingId, adminToken); } catch { /* ignore */ }

            throw new Xunit.Sdk.XunitException($"Expected UI error 'Failed to create booking' but not found. See artifacts. Exception: {ex.Message}");
        }

        // Teardown: remove the API-created booking
        try { await bookingClient.DeleteBookingAsync(apiBookingId, adminToken); } catch { /* ignore */ }
    }
    [Theory(DisplayName = "UI-14 - Lastname accepts injection-like strings as plain text without breaking UI")]
    [InlineData("scriptPayload", "<script>alert(1)</script>")]
    [InlineData("imgPayload", "\"><img src=x onerror=alert(1)>")]
    public void BookingForm_LastName_InjectionLikeInput_TreatedAsPlainText(string caseId, string payload)
    {
        // Arrange
        _home.GoTo();
        _home.IsLoaded().Should().BeTrue();

        // Open booking UI for a specific room card
        _home.ClickBookNowForRoom("Suite");
        _room.ClickReserveNow();

        // Build contact with valid firstname (<= 18 chars) and injection-like lastname
        var contact = new Contact
        {
            FirstName = "ValidFirst",         // within firstname length limit
            LastName = payload,               // injection-like string
            Email = "xss-tester@example.com",
            Phone = "12345678901"             // valid phone to avoid phone validation block
        };

        // Act – fill form and submit
        _room.FillContactForm(contact);
        _room.ClickReserveNowForBooking(); // submit booking

        // Assert – UI should not break and should treat data as plain text
        try
        {
            var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(_fix.Driver, TimeSpan.FromSeconds(10));

            // 1) Booking Confirmed card should appear (no application crash)
            var heading = wait.Until(d =>
            {
                try
                {
                    return d.FindElements(By.XPath("//h2[normalize-space()='Booking Confirmed']"))
                            .FirstOrDefault(e => e.Displayed);
                }
                catch { return null; }
            });

            heading.Should().NotBeNull($"Booking Confirmed heading should appear after successful booking in case '{caseId}'");

            // 2) Page should NOT show generic application error
            var appError = _fix.Driver.FindElements(By.XPath("//*[contains(text(),'Application error')]"))
                                      .FirstOrDefault(e => e.Displayed);
            appError.Should().BeNull($"UI should not crash with 'Application error' when using payload in lastname (case '{caseId}')");

            // 3) The payload should be present in the page source as text (no script execution)
            var pageSource = _fix.Driver.PageSource;
            pageSource.Should().Contain(payload, $"the payload for case '{caseId}' should be persisted/displayed as text, not executed");
        }
        catch (Exception ex)
        {
            // On any unexpected behavior, capture artifacts for triage
            var ts = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

            try
            {
                var ss = ((ITakesScreenshot)_fix.Driver).GetScreenshot();
                var screenshotFile = $"booking_xss_lastname_{caseId}_{ts}.png";
                System.IO.File.WriteAllBytes(screenshotFile, ss.AsByteArray);
                _output.WriteLine($"Saved screenshot: {screenshotFile}");
            }
            catch (Exception se) { _output.WriteLine($"Screenshot failed ({caseId}): {se.Message}"); }

            try
            {
                var pageFile = $"booking_xss_lastname_{caseId}_{ts}.html";
                System.IO.File.WriteAllText(pageFile, _fix.Driver.PageSource);
                _output.WriteLine($"Saved page source: {pageFile}");
            }
            catch (Exception pe) { _output.WriteLine($"Save page source failed ({caseId}): {pe.Message}"); }

            try
            {
                var logs = new List<string>();
                var entries = _fix.Driver.Manage().Logs.GetLog(LogType.Browser);
                logs.AddRange(entries.Select(e => $"{e.Timestamp} {e.Level} {e.Message}"));
                var logFile = $"booking_xss_lastname_{caseId}_console_{ts}.log";
                System.IO.File.WriteAllLines(logFile, logs);
                _output.WriteLine($"Saved console logs: {logFile}");
            }
            catch (Exception le) { _output.WriteLine($"Collect console logs failed ({caseId}): {le.Message}"); }

            throw new Xunit.Sdk.XunitException(
                $"UI did not handle injection-like input in lastname as expected for case '{caseId}'. See artifacts. Exception: {ex.Message}");
        }
    }
}