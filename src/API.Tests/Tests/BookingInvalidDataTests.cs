using System.Text.Json;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

public class BookingInvalidDataTests : TestBase
{
    private readonly ITestOutputHelper _output;
    public BookingInvalidDataTests(ITestOutputHelper output) => _output = output;
    [Theory]
    [InlineData("0NaN-aN-aN")]
    [InlineData("invalid-date")]
    [InlineData("")]
    [InlineData(null)]
    public async Task CreateBooking_InvalidDateVariants_ReturnClientOrServerError(string invalidDate)
    {
        // Arrange
        var booking = CreateRandomBooking();
        booking.bookingdates = new BookingDates { checkin = invalidDate, checkout = invalidDate };
        var client = new BookingClient();

        // Act
        var resp = await client.CreateBookingAsync(booking);

        // Log request/response for triage
        var json = JsonSerializer.Serialize(booking, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        _output.WriteLine($"Request JSON: {json}");
        _output.WriteLine($"Status: {(int)resp.StatusCode} - {resp.StatusCode}");
        _output.WriteLine($"Response: {resp.Content}");

        // Assert: prefer 4xx; if 500 -> fail with clear message for bug report
        var code = (int)resp.StatusCode;
        if (code >= 400 && code < 500)
        {
            code.Should().BeInRange(400, 499);
        }
        else if (code == 500)
        {
            throw new Xunit.Sdk.XunitException($"Server returned 500 for invalid date '{invalidDate}'. Response: {resp.Content}");
        }
        else
        {
            throw new Xunit.Sdk.XunitException($"Unexpected status {code} for invalid date '{invalidDate}'. Response: {resp.Content}");
        }
    }
    [Theory]
    [InlineData(null, "Doe")]
    [InlineData("", "Doe")]
    [InlineData("John", null)]
    [InlineData("John", "")]
    [InlineData(null, null)]
    [InlineData("", "")]
    public async Task CreateBooking_MissingNameFields_Returns4xxOrServerError(string firstName, string lastName)
    {
        // Arrange
        var booking = CreateRandomBooking();
        booking.firstname = firstName;
        booking.lastname = lastName;
        var client = new BookingClient();
        // Act
        var resp = await client.CreateBookingAsync(booking);

        // Log request/response
        var json = System.Text.Json.JsonSerializer.Serialize(
            booking,
            new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
        _output.WriteLine($"Request JSON: {json}");
        _output.WriteLine($"Status: {(int)resp.StatusCode} - {resp.StatusCode}");
        _output.WriteLine($"Response: {resp.Content}");

        // Assert: prefer 4xx; if 500 -> fail with clear message for triage
        var code = (int)resp.StatusCode;
        if (code >= 400 && code < 500)
        {
            code.Should().BeInRange(400, 499);
        }
        else if (code == 500)
        {
            throw new Xunit.Sdk.XunitException($"Server returned 500 for missing name fields (firstname='{firstName}', lastname='{lastName}'). Response: {resp.Content}");
        }
        else
        {
            throw new Xunit.Sdk.XunitException($"Unexpected status {code} for missing name fields (firstname='{firstName}', lastname='{lastName}'). Response: {resp.Content}");
        }
    }
}