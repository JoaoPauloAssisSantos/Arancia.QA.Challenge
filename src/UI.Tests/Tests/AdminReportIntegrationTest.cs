using Arancia.Test.API.Clients;
using FluentAssertions;
using OpenQA.Selenium;
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

    [Fact(DisplayName = "INT-21 - Booking created via API is visible in Admin Report UI")]
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

}
