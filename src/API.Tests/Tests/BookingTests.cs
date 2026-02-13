using FluentAssertions;
using Xunit.Abstractions;
public class BookingTests : TestBase
{
    private readonly ITestOutputHelper _output;
    public BookingTests(ITestOutputHelper output)
    {
        _output = output;
    }
    [Fact]
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
        doc.RootElement.TryGetProperty("booking", out _).Should().BeTrue();
    }

    [Fact]
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
        root.TryGetProperty("booking", out var bookingEl).Should().BeTrue("response must contain booking object");

        // validate returned fields match the sent payload
        var returned = bookingEl;

        returned.GetProperty("firstname").GetString().Should().Be(booking.firstname);
        returned.GetProperty("lastname").GetString().Should().Be(booking.lastname);
        returned.GetProperty("totalprice").GetInt32().Should().Be(booking.totalprice);
        returned.GetProperty("depositpaid").GetBoolean().Should().Be(booking.depositpaid);

        // bookingdates fields
        var returnedDates = returned.GetProperty("bookingdates");
        returnedDates.GetProperty("checkin").GetString().Should().Be(booking.bookingdates!.checkin);
        returnedDates.GetProperty("checkout").GetString().Should().Be(booking.bookingdates!.checkout);

        // optional: additional needs
        if (booking.additionalneeds is not null)
            returned.GetProperty("additionalneeds").GetString().Should().Be(booking.additionalneeds);

        // Log identifiers for debug
        var bookingId = idEl.GetInt32();
        _output.WriteLine($"Created booking id: {bookingId}");
        _output.WriteLine($"Response body: {resp.Content}");

        // Teardown (delete created booking) — implement DeleteBookingAsync and auth if required

    }
    

}
