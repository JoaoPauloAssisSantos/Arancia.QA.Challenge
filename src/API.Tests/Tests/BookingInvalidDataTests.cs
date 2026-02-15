using System.Net;
using FluentAssertions;
using Xunit.Abstractions;

public class BookingInvalidDataTests : TestBase
{
    private readonly ITestOutputHelper _output;
    public BookingInvalidDataTests(ITestOutputHelper output) => _output = output;
    [Theory(DisplayName = "API-04 — Create booking with invalid date")]
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

        // Log
        BookingTestHelper.LogRequestResponse(_output, booking, resp);

        // Assert: prefer 4xx; 500 -> explicit failure; otherwise fail with informative message
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

    [Theory(DisplayName = "API-04 — Create booking with invalid name")]
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

        // Log
        BookingTestHelper.LogRequestResponse(_output, booking, resp);

        var code = (int)resp.StatusCode;

        if (code >= 400 && code < 500)
        {
            resp.Content.Should().NotBeNullOrWhiteSpace("client should return error details for invalid payload");

            var errors = BookingTestHelper.ParseErrors(resp.Content);
            BookingTestHelper.AssertValidationMessagesForNames(errors, firstName, lastName);
        }
        else if (code == 500)
        {
            throw new Xunit.Sdk.XunitException($"Server returned 500 for missing name fields (firstname='{firstName}', lastname='{lastName}'). Response: {resp.Content}");
        }
        else if (code == 200)
        {
            throw new Xunit.Sdk.XunitException($"Unexpected 200 OK for invalid payload (firstname='{firstName}', lastname='{lastName}'). Response: {resp.Content}");
        }
        else
        {
            throw new Xunit.Sdk.XunitException($"Unexpected status {code} for missing name fields (firstname='{firstName}', lastname='{lastName}'). Response: {resp.Content}");
        }
    }
}