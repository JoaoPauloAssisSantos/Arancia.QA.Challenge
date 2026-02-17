using Arancia.Test.API.Clients;
using Arancia.Test.API.Helpers;
using FluentAssertions;
using RestSharp;
using System.Net;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

public class BookingListTests : TestBase
{
    public BookingListTests(ITestOutputHelper output) => InitTestBase(output);
    [Fact(DisplayName = "API-05 - Retrieve list of existing bookings")]
    public async Task GetBookings_ShouldReturnListWithBookingId()
    {
        // Preconditions
        Output.Should().NotBeNull();

        // ARRANGE – ensure at least one booking exists for roomid=1 (or another room)
        var booking = CreateRandomBooking();
        booking.roomid = 1;

        var bookingClient = new BookingClient();
        var createResp = await bookingClient.CreateBookingAsync(booking);
        BookingTestHelper.LogRequestResponse(Output, "POST /booking (precondition)", createResp);

        var status = createResp.StatusCode;
        var code = (int)status;

        if (status == HttpStatusCode.OK || status == HttpStatusCode.Created)
        {
            // happy path: we created a new booking
            createResp.Content.Should().NotBeNullOrWhiteSpace();

            using var createdDoc = JsonDocument.Parse(createResp.Content!);
            createdDoc.RootElement.TryGetProperty("bookingid", out var createdIdEl).Should().BeTrue();
            var createdId = createdIdEl.GetInt32();
            Output.WriteLine($"[Precondition] Created booking id: {createdId}, roomid: {booking.roomid}");
        }
        else if (status == HttpStatusCode.Conflict)
        {
            // 409 Conflict: a booking already exists for these room/dates.
            // For API-05 we only need "at least one booking exists", so this is acceptable.
            Output.WriteLine($"[Precondition] Booking creation returned 409 Conflict (probably room/dates already booked). Body: {createResp.Content}");
        }
        else
        {
            throw new Xunit.Sdk.XunitException(
                $"[Precondition] Unexpected status {code} when creating booking for API-05. Response: {createResp.Content}");
        }

        // Get token for the filtered GET (if required by this endpoint)
        var auth = new AutomationTestingAuthClient();
        var token = await auth.GetTokenAsync("admin", "password");
        Output.WriteLine($"Token: {token}");

        // ACT – GET /booking?roomid={roomid}
        var client = ApiClientFactory.Create(Settings.AutomationTestingApiBase);
        var req = new RestRequest("booking", Method.Get)
            .AddQueryParameter("roomid", booking.roomid.ToString())
            .AddHeader("Accept", "*/*")
            .AddHeader("Referer", "")
            .AddHeader("Cookie", $"token={token}");

        var resp = await client.ExecuteAsync(req);

        // Log
        BookingTestHelper.LogRequestResponse(Output, $"GET /booking?roomid={booking.roomid}", resp);

        // ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.OK, "GET /booking?roomid should return 200");
        resp.Content.Should().NotBeNullOrWhiteSpace();

        using var doc = JsonDocument.Parse(resp.Content!);
        var root = doc.RootElement;

        root.TryGetProperty("bookings", out var bookingsEl)
            .Should().BeTrue("'bookings' array expected in response");
        bookingsEl.ValueKind.Should().Be(JsonValueKind.Array);

        var matchFound = bookingsEl.EnumerateArray()
            .Where(el => el.ValueKind == JsonValueKind.Object)
            .Any(el =>
                el.TryGetProperty("roomid", out var rEl) && rEl.GetInt32() == booking.roomid &&
                el.TryGetProperty("firstname", out var fEl) && !string.IsNullOrWhiteSpace(fEl.GetString()) &&
                el.TryGetProperty("lastname", out var lEl) && !string.IsNullOrWhiteSpace(lEl.GetString()) &&
                el.TryGetProperty("depositpaid", out var dEl) && (dEl.ValueKind == JsonValueKind.True || dEl.ValueKind == JsonValueKind.False) &&
                el.TryGetProperty("bookingdates", out var bdEl) && bdEl.ValueKind == JsonValueKind.Object
            );

        matchFound.Should().BeTrue("expected at least one booking for the requested roomid with core fields present");
    }

    [Fact(DisplayName = "API-06 - Get booking by existing ID")]
    public async Task GetBooking_ByExistingId_ReturnsCorrectBookingDetails()
    {
        // Arrange - create booking
        var booking = CreateRandomBooking();
        var bookingClient = new BookingClient();
        var createResp = await bookingClient.CreateBookingAsync(booking);
        createResp.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        createResp.Content.Should().NotBeNullOrWhiteSpace();

        using var createdDoc = JsonDocument.Parse(createResp.Content!);
        createdDoc.RootElement.TryGetProperty("bookingid", out var createdIdEl).Should().BeTrue();
        var createdId = createdIdEl.GetInt32();
        Output.WriteLine($"Created booking id: {createdId}");

        // Get token
        var auth = new AutomationTestingAuthClient();
        var token = await auth.GetTokenAsync("admin", "password");
        Output.WriteLine($"Token: {token}");

        // Use BookingApiHelper to GET /booking/{id}
        var helper = new BookingApiHelper();
        var getResp = await helper.GetBookingRawAsync(createdId, token);

        BookingTestHelper.LogRequestResponse(Output, $"GET /booking/{createdId}", getResp);

        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        getResp.Content.Should().NotBeNullOrWhiteSpace();

        using var doc = JsonDocument.Parse(getResp.Content!);
        var root = doc.RootElement;

        root.GetProperty("firstname").GetString().Should().Be(booking.firstname);
        root.GetProperty("lastname").GetString().Should().Be(booking.lastname);
        root.GetProperty("depositpaid").GetBoolean().Should().Be(booking.depositpaid);

        var returnedDates = root.GetProperty("bookingdates");
        returnedDates.GetProperty("checkin").GetString().Should().Be(booking.bookingdates!.checkin);
        returnedDates.GetProperty("checkout").GetString().Should().Be(booking.bookingdates!.checkout);
    }

    [Fact(DisplayName = "API-07 A - Get booking by non-existing booking ID")]
    public async Task GetBooking_ByNonExistingBookingId_ReturnsNotFound()
    {
        // Arrange
        const int nonExistingId = 99999999;
        var helper = new BookingApiHelper();
        // Act
        var resp = await helper.GetBookingRawAsync(nonExistingId);

        // Log
        BookingTestHelper.LogRequestResponse(Output, $"GET /booking/{nonExistingId}", resp);

        // Accept either 404 NotFound or 401 Unauthorized with authentication message
        if (resp.StatusCode == HttpStatusCode.NotFound)
        {
            resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
            return;
        }

        if (resp.StatusCode == HttpStatusCode.Unauthorized)
        {
            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            resp.Content.Should().NotBeNullOrWhiteSpace();
            using var doc = JsonDocument.Parse(resp.Content!);
            doc.RootElement.TryGetProperty("error", out var err).Should().BeTrue("expected 'error' in unauthorized response");
            err.GetString().Should().MatchRegex("(?i)auth"); // contains auth/authentication
            return;
        }

        // Otherwise fail with informative message
        throw new Xunit.Sdk.XunitException($"Unexpected status {(int)resp.StatusCode} for non-existing booking id {nonExistingId}. Response: {resp.Content}");

    }

    [Fact(DisplayName = "API-07 B - Get booking by non-existing room ID")]
    public async Task GetBooking_ByNonExistingRoomId_ReturnsNotFound()
    {
        // Arrange
        const int nonExistingRoomId = 99999999;
        var client = ApiClientFactory.Create(Settings.AutomationTestingApiBase);
        var auth = new AutomationTestingAuthClient();
        var token = await auth.GetTokenAsync("admin", "password");
        var req = new RestRequest("booking", Method.Get)
            .AddQueryParameter("roomid", nonExistingRoomId.ToString())
            .AddHeader("Accept", "*/*")
            .AddHeader("Referer", "")
            .AddHeader("Cookie", $"token={token}");

        // Act
        var resp = await client.ExecuteAsync(req);

        // Log
        Output.WriteLine($"GET /booking?roomid={nonExistingRoomId} status: {(int)resp.StatusCode} - {resp.StatusCode}");
        Output.WriteLine($"Body: {resp.Content ?? "<null>"}");

        // Assert: accept either 404 NotFound or 200 with empty "bookings" array
        if (resp.StatusCode == HttpStatusCode.NotFound)
        {
            resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
            return;
        }

        resp.StatusCode.Should().Be(HttpStatusCode.OK, "if not 404 the API should return 200 with a bookings array");
        resp.Content.Should().NotBeNullOrWhiteSpace();

        using var doc = JsonDocument.Parse(resp.Content!);
        var root = doc.RootElement;

        root.TryGetProperty("bookings", out var bookingsEl).Should().BeTrue("response must contain 'bookings' array");
        bookingsEl.ValueKind.Should().Be(JsonValueKind.Array);

        bookingsEl.GetArrayLength().Should().Be(0, "no bookings should be returned for a non-existing room id");
    }
}