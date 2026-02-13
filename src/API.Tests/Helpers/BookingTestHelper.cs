using System.Net;
using System.Text.Json;
using FluentAssertions;
using RestSharp;

public static class BookingTestHelper
{
    /// <summary>
    /// Creates a booking using the provided BookingClient and Booking instance,
    /// asserts success, and returns the created id + the booking used.
    /// The caller is responsible for generating the booking object (e.g. via TestBase.CreateRandomBooking()).
    /// </summary>
    public static async Task<int> CreateBookingAndGetIdAsync(BookingClient bookingClient, Booking booking)
    {
        var resp = await bookingClient.CreateBookingAsync(booking);

        resp.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        resp.Content.Should().NotBeNullOrEmpty();

        using var doc = JsonDocument.Parse(resp.Content!);
        doc.RootElement.TryGetProperty("bookingid", out var idEl)
            .Should().BeTrue("create response must include bookingid");

        var id = idEl.GetInt32();
        return id;
    }

    /// <summary>
    /// Raw GET /booking/{id} wrapper.
    /// </summary>
    public static async Task<RestResponse> GetBookingByIdAsync(int id)
    {
        var client = ApiClientFactory.Create(Settings.ApiBaseUrl);
        var req = new RestRequest($"booking/{id}", Method.Get);
        return await client.ExecuteAsync(req);
    }
    public static async Task<RestResponse> GetBookingByIdWithRetryAsync(int id, int maxAttempts = 3, int delayMs = 500)
    {
        RestResponse? last = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            last = await GetBookingByIdAsync(id);

            // If it's 200 or 404 etc., we stop retrying
            if (last.StatusCode != (HttpStatusCode)418)
                return last;

            // If 418, wait and retry
            await Task.Delay(delayMs);
        }

        // Return last response (likely still 418)
        return last!;
    }
}
