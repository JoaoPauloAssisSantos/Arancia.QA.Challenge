using Arancia.Test.API.Clients;
using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using UI.Tests.Fixture;
using UI.Tests.Helpers;
using UI.Tests.Pages;
using Xunit.Abstractions;

public class AdminReportIntegrationTests : IClassFixture<WebDriverFixture>
{
    private readonly WebDriverFixture _fix;
    private readonly ITestOutputHelper _output;
    private readonly AdminAuthPage _adminAuth;
    private readonly AdminReportPage _adminReport;
    private readonly string _baseUrl = "https://automationintesting.online";

    public AdminReportIntegrationTests(WebDriverFixture fix, ITestOutputHelper output)
    {
        _fix = fix ?? throw new ArgumentNullException(nameof(fix));
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _adminAuth = new AdminAuthPage(_fix.Driver, "https://automationintesting.online");
        _adminReport = new AdminReportPage(_fix.Driver, _baseUrl);
    }

    [Fact(DisplayName = "UI-11 - Booking created via API is visible in Admin Report UI")]
    public async Task BookingCreatedViaApi_IsVisible_InAdminReport()
    {
        // ARRANGE – create booking via API
        var apiBase = Arancia.Test.API.Helpers.Settings.AutomationTestingApiBase;
        var bookingClient = new BookingClient(ApiClientFactory.Create(apiBase));

        // Known mapping: roomid 1 == RoomName "101" (from earlier API responses)
        const int targetRoomId = 1;
        const string targetRoomName = "101";

        var booking = BookingFactory.CreateRandomBooking();
        booking.roomid = targetRoomId;

        // Use a near-future date so it is easy to navigate to that month
        var rnd = new Random();
        var offsetDays = rnd.Next(5, 31);         // 5–30 days from today
        var lengthDays = rnd.Next(2, 6);          // 2–5 nights
        var checkin = DateTime.UtcNow.Date.AddDays(offsetDays);
        var checkout = checkin.AddDays(lengthDays);

        booking.bookingdates!.checkin = checkin.ToString("yyyy-MM-dd");
        booking.bookingdates!.checkout = checkout.ToString("yyyy-MM-dd");

        var apiCreateResp = await bookingClient.CreateBookingAsync(booking);
        BookingTestHelper.LogRequestResponse(_output, "POST /booking (INT-21)", apiCreateResp);
        apiCreateResp.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);

        using var apiDoc = JsonDocument.Parse(apiCreateResp.Content!);
        apiDoc.RootElement.TryGetProperty("bookingid", out var idEl).Should().BeTrue();
        var bookingId = idEl.GetInt32();
        var fullName = $"{booking.firstname} {booking.lastname}";

        // ACT – login as admin and go to Report via nav menu
        _adminAuth.GoToLogin();
        _adminAuth.LoginAsAdmin();
        _adminReport.ClickReport();

        // Ensure the calendar is on the month of the checkin date
        _adminReport.NavigateToMonth(checkin);

        // ASSERT – booking appears as an event "FullName - Room: 101"
        var evt = _adminReport.WaitForBookingEvent(fullName, targetRoomName, timeoutSeconds: 30);
        if (evt == null)
        {
            // capture artifacts for triage
            var ts = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            try
            {
                var ss = ((ITakesScreenshot)_fix.Driver).GetScreenshot();
                var screenshotFile = $"int21_booking_report_not_found_{ts}.png";
                File.WriteAllBytes(screenshotFile, ss.AsByteArray);
                _output.WriteLine($"Saved screenshot: {screenshotFile}");
            }
            catch (Exception se) { _output.WriteLine($"Screenshot failed: {se.Message}"); }

            try
            {
                var pageFile = $"int21_booking_report_not_found_{ts}.html";
                File.WriteAllText(pageFile, _fix.Driver.PageSource);
                _output.WriteLine($"Saved page source: {pageFile}");
            }
            catch (Exception pe) { _output.WriteLine($"Save page source failed: {pe.Message}"); }

            try
            {
                var logs = new List<string>();
                var entries = _fix.Driver.Manage().Logs.GetLog(LogType.Browser);
                logs.AddRange(entries.Select(e => $"{e.Timestamp} {e.Level} {e.Message}"));
                var logFile = $"int21_booking_report_not_found_{ts}_console.log";
                System.IO.File.WriteAllLines(logFile, logs);
                _output.WriteLine($"Saved console logs: {logFile}");
            }
            catch (Exception le) { _output.WriteLine($"Collect console logs failed: {le.Message}"); }

            throw new Xunit.Sdk.XunitException(
                $"Expected booking '{fullName} - Room: {targetRoomName}' to appear in Admin Report UI, but it was not found.");
        }

        evt.Should().NotBeNull("booking event created via API should be visible in Admin Report UI");
        evt!.Text.Trim().Should().Be($"{fullName} - Room: {targetRoomName}");

        // TEARDOWN – delete booking via API (best-effort)
        try
        {
            var adminAuthApi = new AutomationTestingAuthClient();
            var adminToken = await adminAuthApi.GetTokenAsync("admin", "password");
            var delResp = await bookingClient.DeleteBookingAsync(bookingId, adminToken);
            _output.WriteLine($"Cleanup DELETE /booking/{bookingId} => {(int)delResp.StatusCode} - {delResp.StatusCode}");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Cleanup booking {bookingId} failed: {ex.Message}");
        }
    }
    [Fact(DisplayName = "UI-19 - Report calendar remains responsive with many bookings")]
    public async Task ReportCalendar_Performance_WithManyBookings()
    {
        // ARRANGE – populate many bookings via API for a target month
        var apiBase = Arancia.Test.API.Helpers.Settings.AutomationTestingApiBase;
        var bookingClient = new BookingClient(ApiClientFactory.Create(apiBase));

        // Use admin token for cleanup if needed (optional)
        var adminAuthApi = new AutomationTestingAuthClient();
        var adminToken = await adminAuthApi.GetTokenAsync("admin", "password");
        adminToken.Should().NotBeNullOrWhiteSpace();

        var rnd = new Random();

        // Target month: next month (to avoid mixing with existing demo data)
        var today = DateTime.UtcNow.Date;
        var firstOfNextMonth = new DateTime(today.Year, today.Month, 1).AddMonths(1);
        var targetYear = firstOfNextMonth.Year;
        var targetMonth = firstOfNextMonth.Month;

        const int bookingsToCreate = 20;
        var createdBookingIds = new System.Collections.Generic.List<int>();

        for (int i = 0; i < bookingsToCreate; i++)
        {
            // random day within the target month (1–28 to be safe)
            var day = rnd.Next(1, 29);
            var checkin = new DateTime(targetYear, targetMonth, day);
            var lengthDays = rnd.Next(1, 4); // 1–3 nights
            var checkout = checkin.AddDays(lengthDays);

            var booking = BookingFactory.CreateRandomBooking();
            booking.roomid = rnd.Next(1, 4); // spread across a few rooms
            booking.bookingdates!.checkin = checkin.ToString("yyyy-MM-dd");
            booking.bookingdates!.checkout = checkout.ToString("yyyy-MM-dd");

            var resp = await bookingClient.CreateBookingAsync(booking);
            BookingTestHelper.LogRequestResponse(_output, $"POST /booking (perf-seed #{i + 1})", resp);

            if (resp.StatusCode == HttpStatusCode.OK || resp.StatusCode == HttpStatusCode.Created)
            {
                using var doc = JsonDocument.Parse(resp.Content!);
                doc.RootElement.TryGetProperty("bookingid", out var idEl).Should().BeTrue();
                createdBookingIds.Add(idEl.GetInt32());
            }
            else if (resp.StatusCode == HttpStatusCode.Conflict)
            {
                // booking already exists for that room/dates – log & continue
                _output.WriteLine($"Seed #{i + 1}: 409 Conflict (probably duplicate room/date combo). Body: {resp.Content}");
                // do not add to createdBookingIds; just move on
            }
            else
            {
                // unexpected error – fail fast
                throw new Xunit.Sdk.XunitException(
                    $"Unexpected status {(int)resp.StatusCode} during perf seeding. Body: {resp.Content}");
            }
        }

        _output.WriteLine($"Seeded {createdBookingIds.Count} bookings for {firstOfNextMonth:MMMM yyyy}.");

        // ACT – measure time to open Report, navigate to target month, and see events
        var stopwatch = Stopwatch.StartNew();

        _adminAuth.GoToLogin();
        _adminAuth.LoginAsAdmin();
        _adminReport.ClickReport(); // assumes this clicks the "Report" nav link

        // navigate calendar to the seeded month
        _adminReport.NavigateToMonth(firstOfNextMonth);

        // Wait until at least one event is visible (we know we created many)
        var wait = new WebDriverWait(_fix.Driver, TimeSpan.FromSeconds(15));
        IWebElement? anyEvent = null;
        try
        {
            anyEvent = wait.Until(d =>
            {
                try
                {
                    var events = d.FindElements(By.CssSelector(".rbc-event-content"));
                    return events.FirstOrDefault(e => e.Displayed);
                }
                catch { return null; }
            });
        }
        catch (WebDriverTimeoutException)
        {
            // will be asserted below
        }

        stopwatch.Stop();
        var elapsed = stopwatch.Elapsed;
        _output.WriteLine($"Report page + navigation + render time: {elapsed.TotalSeconds:F2}s");

        // ASSERT – UI behaves within acceptable range and remains responsive

        // 1) At least one event is visible for that month
        anyEvent.Should().NotBeNull("At least one booking event should be visible on the report calendar after seeding many bookings");

        // 2) Basic performance gate (tune this threshold based on CI/env characteristics)
        // For a public demo env, 3s might be optimistic; use a slightly relaxed threshold (e.g. 8s)
        elapsed.TotalSeconds.Should().BeLessThan(8,
            "Report calendar should remain responsive and render bookings within a reasonable time with many records present");

        // 3) Calendar still responds to user actions (e.g., clicking Next and Back without throwing)
        try
        {
            _adminReport.NavigateToMonth(firstOfNextMonth.AddMonths(1)); // go to next month
            _adminReport.NavigateToMonth(firstOfNextMonth);             // and back to target month
        }
        catch (Exception ex)
        {
            throw new Xunit.Sdk.XunitException($"Calendar navigation became unresponsive after seeding many bookings. Exception: {ex.Message}");
        }

        // 4) Optional: check console logs for severe JS errors
        try
        {
            var logs = _fix.Driver.Manage().Logs.GetLog(LogType.Browser);
            var severe = logs.Where(l => l.Level == OpenQA.Selenium.LogLevel.Severe).ToList();
            if (severe.Any())
            {
                _output.WriteLine("Browser severe logs detected during performance test:");
                foreach (var log in severe)
                    _output.WriteLine($"{log.Timestamp} [{log.Level}] {log.Message}");

                // Do not necessarily fail on every SEVERE if the env is noisy; you can choose:
                // severe.Should().BeEmpty("No severe browser errors are expected during report rendering with many bookings.");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Could not collect browser console logs (not critical): {ex.Message}");
        }

        // TEARDOWN (optional/best-effort) – delete seeded bookings to keep environment cleaner
        foreach (var id in createdBookingIds)
        {
            try
            {
                var delResp = await bookingClient.DeleteBookingAsync(id, adminToken);
                _output.WriteLine($"Cleanup DELETE /booking/{id} => {(int)delResp.StatusCode} - {delResp.StatusCode}");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Cleanup booking {id} failed: {ex.Message}");
            }
        }
    }
}
