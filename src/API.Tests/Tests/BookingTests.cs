using Xunit;
using FluentAssertions;
public class BookingTests : TestBase
{
    [Fact]
    public async System.Threading.Tasks.Task CreateBooking_HappyPath_ReturnsBookingId()
    {
        var booking = CreateRandomBooking();
        var client = new BookingClient();
        var resp = await client.CreateBookingAsync(booking);
        resp.StatusCode.Should().BeOneOf(System.Net.HttpStatusCode.OK, System.Net.HttpStatusCode.Created); 
        resp.Content.Should().NotBeNullOrEmpty();
        using var doc = System.Text.Json.JsonDocument.Parse(resp.Content!);
        doc.RootElement.TryGetProperty("bookingid", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("booking", out _).Should().BeTrue();
    }
}
