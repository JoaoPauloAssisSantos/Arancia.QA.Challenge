using Arancia.Test.API.Clients;
using Arancia.Test.API.Helpers;
using FluentAssertions;
using Xunit.Abstractions;
public class BookingTests : TestBase
{
    private readonly ITestOutputHelper _output;
    public BookingTests(ITestOutputHelper output)
    {
        _output = output;
    }
    [Fact(DisplayName = "API-02 — Create booking (happy path)")]
    public async Task CreateBooking_HappyPath_ReturnsBookingId()
    {
        // Arrange
        var booking = CreateRandomBooking();
        var client = new BookingClient();
        // Act
        var resp = await client.CreateBookingAsync(booking);
        // Assert
        resp.StatusCode.Should().BeOneOf(System.Net.HttpStatusCode.OK, System.Net.HttpStatusCode.Created);
        resp.Content.Should().NotBeNullOrEmpty();
        using var doc = System.Text.Json.JsonDocument.Parse(resp.Content!);
        doc.RootElement.TryGetProperty("bookingid", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("bookingdates", out _).Should().BeTrue();
    }

    [Fact(DisplayName = "API-02 — Create booking and returns matches Payload(happy path)")]
    public async Task CreateBooking_HappyPath_ReturnsBookingId_AndMatchesPayload()
    {
        // Arrange
        var booking = CreateRandomBooking();
        var client = new BookingClient();
        // Act
        var resp = await client.CreateBookingAsync(booking);

        // Assert - status with response in failure message
        resp.StatusCode
            .Should()
            .BeOneOf(new[] { System.Net.HttpStatusCode.OK, System.Net.HttpStatusCode.Created },
                because: $"Unexpected status {(int)resp.StatusCode}. Response: {resp.Content}");

        resp.Content.Should().NotBeNullOrEmpty();

        using var doc = System.Text.Json.JsonDocument.Parse(resp.Content!);
        var root = doc.RootElement;

        // bookingid present
        root.TryGetProperty("bookingid", out var idEl).Should().BeTrue("response must contain bookingid");

        // booking object present
        root.TryGetProperty("bookingdates", out var bookingEl).Should().BeTrue("response must contain booking object");

        // validate returned fields match the sent payload
        var returned = bookingEl;

        root.GetProperty("roomid").GetInt32().Should().Be(booking.roomid);
        root.GetProperty("firstname").GetString().Should().Be(booking.firstname);
        root.GetProperty("lastname").GetString().Should().Be(booking.lastname);
        root.GetProperty("depositpaid").GetBoolean().Should().Be(booking.depositpaid);

        // bookingdates fields
        var returnedDates = root.GetProperty("bookingdates");
        returnedDates.GetProperty("checkin").GetString().Should().Be(booking.bookingdates!.checkin);
        returnedDates.GetProperty("checkout").GetString().Should().Be(booking.bookingdates!.checkout);

        // Log identifiers for debug
        var bookingId = idEl.GetInt32();
        _output.WriteLine($"Created booking id: {bookingId}");
        _output.WriteLine($"Response body: {resp.Content}");
    }
    

}
