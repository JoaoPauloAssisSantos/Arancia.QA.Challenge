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

    [Fact(DisplayName = "UI-XX - Admin can create room and it appears in UI and API")]
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
}
