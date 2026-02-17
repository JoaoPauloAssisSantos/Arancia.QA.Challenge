using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using Arancia.Test.API.Clients;
using Arancia.Test.API.Helpers;
using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using UI.Tests.Fixture;
using UI.Tests.Pages;
using Xunit;
using Xunit.Abstractions;

public class AdminRoomsTests : IClassFixture<WebDriverFixture>
{
    private readonly WebDriverFixture _fix;
    private readonly ITestOutputHelper _output;
    private readonly AdminAuthPage _adminAuth;
    private readonly AdminRoomsPage _adminRooms;

    public AdminRoomsTests(WebDriverFixture fix, ITestOutputHelper output)
    {
        _fix = fix ?? throw new ArgumentNullException(nameof(fix));
        _output = output ?? throw new ArgumentNullException(nameof(output));
        var baseUrl = "https://automationintesting.online";
        _adminAuth = new AdminAuthPage(_fix.Driver, baseUrl);
        _adminRooms = new AdminRoomsPage(_fix.Driver, baseUrl);
    }

    [Fact(DisplayName = "UI-12 - Admin can create room via UI and it appears in UI and API")]
    public async Task Admin_CreateRoom_ShowsInUiAndApi()
    {
        var wait = new WebDriverWait(_fix.Driver, TimeSpan.FromSeconds(10));
        var apiBase = Arancia.Test.API.Helpers.Settings.AutomationTestingApiBase;

        // Arrange: login and go to /admin/rooms
        _adminAuth.GoToLogin();
        _adminAuth.LoginAsAdmin();

        // Generate random values between 100 and 250
        var rnd = new Random();
        var roomNumber = rnd.Next(100, 251);
        var roomPrice = rnd.Next(100, 251);

        // Act: create room via UI
        _adminRooms.CreateRoom(roomNumber, roomPrice);

        // Assert (UI) – row added on screen
        var row = _adminRooms.WaitForRoomRow(roomNumber, roomPrice, timeoutSeconds: 10);
        row.Should().NotBeNull($"a row with roomName {roomNumber} and price {roomPrice} should be visible in the rooms listing");

        var cells = row!.FindElements(By.CssSelector("div.col-sm-1, div.col-sm-2, div.col-sm-5")).ToList();
        cells.Should().NotBeEmpty();

        // Room # (primeira coluna)
        var roomNumberText = cells[0].Text.Trim();
        roomNumberText.Should().Be(roomNumber.ToString());

        // Price 
        var priceText = row.FindElement(By.CssSelector($"p#roomPrice{roomPrice}")).Text.Trim();
        priceText.Should().Be(roomPrice.ToString());

        // Assert (API) – new room appears in GET /room
        var adminAuthApi = new AutomationTestingAuthClient();
        var adminToken = await adminAuthApi.GetTokenAsync("admin", "password");
        adminToken.Should().NotBeNullOrWhiteSpace();

        var apiRoomClient = new RoomClient(ApiClientFactory.Create(apiBase));
        var roomsResp = await apiRoomClient.GetRoomsAsync(adminToken);

        _output.WriteLine($"GET /room status: {(int)roomsResp.StatusCode} - {roomsResp.StatusCode}");
        _output.WriteLine($"Body: {roomsResp.Content}");

        roomsResp.StatusCode.Should().Be(HttpStatusCode.OK);
        roomsResp.Content.Should().NotBeNullOrWhiteSpace();

        using var roomsDoc = JsonDocument.Parse(roomsResp.Content!);
        roomsDoc.RootElement.TryGetProperty("rooms", out var roomsEl).Should().BeTrue();

        var foundInApi = roomsEl.EnumerateArray().Any(r =>
        {
            try
            {
                var rn = r.GetProperty("roomName").GetString();
                var price = r.GetProperty("roomPrice").GetInt32();
                return rn == roomNumber.ToString() && price == roomPrice;
            }
            catch
            {
                return false;
            }
        });

        foundInApi.Should().BeTrue($"room with roomName '{roomNumber}' and price '{roomPrice}' should be returned by GET /room");
    }
    [Fact(DisplayName = "UI-13 - Admin creates room via POST API and it appears in UI and API")]
    public async Task Admin_CreateRoomViaApi_ShowsInUiAndApi()
    {
        var wait = new WebDriverWait(_fix.Driver, TimeSpan.FromSeconds(10));
        var apiBase = Arancia.Test.API.Helpers.Settings.AutomationTestingApiBase;

        // ARRANGE (API) – create room via API (POST /api/room)
        var adminAuthApi = new AutomationTestingAuthClient();
        var adminToken = await adminAuthApi.GetTokenAsync("admin", "password");
        adminToken.Should().NotBeNullOrWhiteSpace();

        var apiRoomClient = new RoomClient(ApiClientFactory.Create(apiBase));

        // Generate a random numeric roomName and price between 100–250
        var rnd = new Random();
        var roomNumber = rnd.Next(100, 251);       // e.g. 123
        var roomPrice = rnd.Next(100, 251);

        var roomPayload = new Room
        {
            RoomName = roomNumber.ToString(),
            Type = "Single",
            Accessible = true,
            Image = "https://blog.postman.com/wp-content/uploads/2014/07/logo.png",
            Description = $"Room created via API test at {DateTime.UtcNow:o}",
            RoomPrice = roomPrice,
            Features = new[] { "WiFi" }
        };

        var createResp = await apiRoomClient.CreateRoomAsync(roomPayload, adminToken);
        _output.WriteLine($"POST /api/room status: {(int)createResp.StatusCode} - {createResp.StatusCode}");
        _output.WriteLine($"Body: {createResp.Content}");

        createResp.StatusCode.Should().Be(HttpStatusCode.OK);
        createResp.Content.Should().NotBeNullOrWhiteSpace();

        using (var createDoc = JsonDocument.Parse(createResp.Content!))
        {
            createDoc.RootElement.TryGetProperty("success", out var succEl).Should().BeTrue();
            succEl.GetBoolean().Should().BeTrue("Room creation via API should return success=true");
        }

        // ASSERT (API) – room appears in GET /api/room
        var roomsResp = await apiRoomClient.GetRoomsAsync(adminToken);
        _output.WriteLine($"GET /api/room status: {(int)roomsResp.StatusCode} - {roomsResp.StatusCode}");
        _output.WriteLine($"Body: {roomsResp.Content}");

        roomsResp.StatusCode.Should().Be(HttpStatusCode.OK);
        roomsResp.Content.Should().NotBeNullOrWhiteSpace();

        using var roomsDoc = JsonDocument.Parse(roomsResp.Content!);
        roomsDoc.RootElement.TryGetProperty("rooms", out var roomsEl).Should().BeTrue();

        var foundInApi = roomsEl.EnumerateArray().Any(r =>
        {
            try
            {
                var rn = r.GetProperty("roomName").GetString();
                var price = r.GetProperty("roomPrice").GetInt32();
                return rn == roomNumber.ToString() && price == roomPrice;
            }
            catch
            {
                return false;
            }
        });

        foundInApi.Should().BeTrue(
            $"room with roomName '{roomNumber}' and price '{roomPrice}' should be returned by GET /api/room");

        // ACT (UI) – login as admin and go to /admin/rooms
        _adminAuth.GoToLogin();
        _adminAuth.LoginAsAdmin();


        // ASSERT (UI) – row with this roomNumber and roomPrice appears in admin rooms listing
        var row = _adminRooms.WaitForRoomRow(roomNumber, roomPrice, timeoutSeconds: 15);
        if (row == null)
        {
            // capture artifacts for debugging
            var ts = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            try
            {
                var ss = ((ITakesScreenshot)_fix.Driver).GetScreenshot();
                var screenshotFile = $"ui13_room_not_found_{ts}.png";
                System.IO.File.WriteAllBytes(screenshotFile, ss.AsByteArray);
                _output.WriteLine($"Saved screenshot: {screenshotFile}");
            }
            catch (Exception se) { _output.WriteLine($"Screenshot failed: {se.Message}"); }

            try
            {
                var pageFile = $"ui13_room_not_found_{ts}.html";
                System.IO.File.WriteAllText(pageFile, _fix.Driver.PageSource);
                _output.WriteLine($"Saved page source: {pageFile}");
            }
            catch (Exception pe) { _output.WriteLine($"Save page source failed: {pe.Message}"); }

            throw new Xunit.Sdk.XunitException(
                $"Expected room '{roomNumber}' with price '{roomPrice}' to appear in /admin/rooms, but no matching row was found.");
        }

        // Additional UI assertions (optional)
        var cells = row!.FindElements(By.CssSelector("div.col-sm-1, div.col-sm-2, div.col-sm-5")).ToList();
        cells.Should().NotBeEmpty("Expected at least one column in the room row");

        // Room # should match
        var roomNumberText = cells[0].Text.Trim();
        roomNumberText.Should().Be(roomNumber.ToString());

        // Price can be validated directly via its dedicated element
        var priceText = row.FindElement(By.CssSelector($"p#roomPrice{roomPrice}")).Text.Trim();
        priceText.Should().Be(roomPrice.ToString());

        // Clean-up (optional / best-effort): you can delete the room via API if DELETE is available
        // to keep the environment tidy. For example:
        // var deleteResp = await apiRoomClient.DeleteRoomAsync(roomId, adminToken);
        // (roomId can be extracted from the room listing or API if needed)
    }
}
