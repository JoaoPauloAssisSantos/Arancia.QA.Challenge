using FluentAssertions;
using RestSharp;
using System.Net;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

public class BookingListTests : TestBase
{
    private readonly ITestOutputHelper _output;
    public BookingListTests(ITestOutputHelper output) => _output = output;

    [Fact(DisplayName = "API-05 - Retrieve list of existing bookings")]
    public async Task GetBookings_ShouldReturnListWithBookingId()
    {
        // Arrange - ensure at least one booking exists
        var booking = CreateRandomBooking();
        var bookingClient = new BookingClient();
        var createResp = await bookingClient.CreateBookingAsync(booking);
        createResp.StatusCode
            .Should()
            .BeOneOf(new[] { System.Net.HttpStatusCode.OK, System.Net.HttpStatusCode.Created },
                because: "precondition: able to create a booking");
        createResp.Content.Should().NotBeNullOrEmpty();
        using var createdDoc = JsonDocument.Parse(createResp.Content!);
        createdDoc.RootElement.TryGetProperty("bookingid", out var createdIdEl).Should().BeTrue();
        var createdId = createdIdEl.GetInt32();

        _output.WriteLine($"Created booking id (precondition): {createdId}");

        // Act - call GET /booking
        var client = ApiClientFactory.Create(Settings.ApiBaseUrl);
        var req = new RestRequest("booking", Method.Get);
        var resp = await client.ExecuteAsync(req);

        _output.WriteLine($"GET /booking status: {(int)resp.StatusCode} - {resp.StatusCode}");
        _output.WriteLine($"GET /booking body: {resp.Content}");

        // Assert
        resp.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        resp.Content.Should().NotBeNullOrEmpty();

        // Expect response to be a JSON array of objects with bookingid
        using var doc = JsonDocument.Parse(resp.Content!);
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array, "GET /booking should return an array");

        bool found = false;
        foreach (var el in doc.RootElement.EnumerateArray())
        {
            if (el.ValueKind != JsonValueKind.Object) continue;
            if (el.TryGetProperty("bookingid", out var idEl) && idEl.ValueKind == JsonValueKind.Number)
            {
                found = true;
                break;
            }
        }
    }
    [Fact(DisplayName = "API-06 - Get booking by existing ID")]
    public async Task GetBooking_ByExistingId_ReturnsCorrectBookingDetails()
    {
        // Arrange (precondition): create a booking
        var booking = CreateRandomBooking();
        var bookingClient = new BookingClient();
        var createResp = await bookingClient.CreateBookingAsync(booking);

        createResp.StatusCode
            .Should()
            .BeOneOf(new[] { System.Net.HttpStatusCode.OK, System.Net.HttpStatusCode.Created },
                because: "precondition: booking must be creatable");

        createResp.Content.Should().NotBeNullOrEmpty();

        using var createdDoc = JsonDocument.Parse(createResp.Content!);
        createdDoc.RootElement.TryGetProperty("bookingid", out var idEl).Should().BeTrue("create response must include bookingid");
        var bookingId = idEl.GetInt32();

        _output.WriteLine($"Created booking id: {bookingId}");

        // Act: GET /booking/{id}
        var client = ApiClientFactory.Create(Settings.ApiBaseUrl);
        var req = new RestRequest($"booking/{bookingId}", Method.Get);
        var getResp = await client.ExecuteAsync(req);

        _output.WriteLine($"GET /booking/{bookingId} status: {(int)getResp.StatusCode} - {getResp.StatusCode}");
        _output.WriteLine($"GET /booking/{bookingId} body: {getResp.Content}");

        // Assert
        getResp.StatusCode.Should().Be(System.Net.HttpStatusCode.OK, because: "GET by existing id should return 200");
        getResp.Content.Should().NotBeNullOrEmpty();

        using var doc = JsonDocument.Parse(getResp.Content!);
        var root = doc.RootElement;

        // Validate core fields
        root.GetProperty("firstname").GetString().Should().Be(booking.firstname);
        root.GetProperty("lastname").GetString().Should().Be(booking.lastname);
        root.GetProperty("totalprice").GetInt32().Should().Be(booking.totalprice);
        root.GetProperty("depositpaid").GetBoolean().Should().Be(booking.depositpaid);

        var returnedDates = root.GetProperty("bookingdates");
        returnedDates.GetProperty("checkin").GetString().Should().Be(booking.bookingdates!.checkin);
        returnedDates.GetProperty("checkout").GetString().Should().Be(booking.bookingdates!.checkout);

        if (booking.additionalneeds is not null)
            root.GetProperty("additionalneeds").GetString().Should().Be(booking.additionalneeds);

        // Optional teardown: delete booking (implement DeleteBookingAsync with auth if available)
        // try { var authToken = await new AuthClient().GetTokenAsync("admin","password"); await bookingClient.DeleteBookingAsync(bookingId, authToken); } catch { /* log but don't fail test */ }
    }
    [Fact(DisplayName = "API-07 - Get booking by non-existing ID")]
    public async Task GetBooking_ByNonExistingId_ReturnsNotFound()
    {
        // Arrange
        const int nonExistingId = 99999999; // high ID, assumed not to exist
        var client = ApiClientFactory.Create(Settings.ApiBaseUrl);

        // Act
        var req = new RestRequest($"booking/{nonExistingId}", Method.Get);
        var resp = await client.ExecuteAsync(req);

        // Log
        _output.WriteLine($"GET /booking/{nonExistingId} status: {(int)resp.StatusCode} - {resp.StatusCode}");
        _output.WriteLine($"Body: {resp.Content}");

        // Assert
        // Comportamento esperado: 404 NotFound
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound,
            $"expected 404 for non-existing booking id {nonExistingId}. Response: {resp.Content}");
    }

}