using System.Net;
using System.Text.Json;
using FluentAssertions;
using RestSharp;
using Xunit.Abstractions;

public static class BookingTestHelper
{
    /// 

    /// Creates a booking using the provided BookingClient and Booking instance, /// asserts success, and returns the created id + the booking used. /// The caller is responsible for generating the booking object (e.g. via TestBase.CreateRandomBooking()). ///
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
        var client = ApiClientFactory.Create(Settings.AutomationTestingBaseUrl);
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
    /// <summary>
    /// Logs request body and response details to the provided test output helper.
    /// </summary>
    public static void LogRequestResponse(ITestOutputHelper output, object requestBody, RestResponse resp)
    {
        var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        output.WriteLine($"Request JSON: {json}");
        output.WriteLine($"Status: {(int)resp.StatusCode} - {resp.StatusCode}");
        output.WriteLine($"Response: {resp.Content}");
    }

    /// <summary>
    /// Parses the "errors" array from a JSON response body. Returns empty list if none found.
    /// </summary>
    public static List<string> ParseErrors(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return new List<string>();

        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            if (!root.TryGetProperty("errors", out var errorsEl) || errorsEl.ValueKind != JsonValueKind.Array)
                return new List<string>();

            return errorsEl.EnumerateArray()
                           .Select(e => e.GetString())
                           .Where(s => !string.IsNullOrWhiteSpace(s))
                           .ToList()!;
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// Asserts that validation messages in errors align with which name fields were missing.
    /// Throws assertion failures via FluentAssertions when expectations are not met.
    /// </summary>
    public static void AssertValidationMessagesForNames(List<string> errors, string? firstName, string? lastName)
    {
        errors.Should().NotBeEmpty("there should be at least one validation message for invalid payload");

        var firstMissing = string.IsNullOrWhiteSpace(firstName);
        var lastMissing = string.IsNullOrWhiteSpace(lastName);

        if (firstMissing)
        {
            errors.Should().Contain(e => e != null &&
                (e.IndexOf("first", StringComparison.OrdinalIgnoreCase) >= 0
                 || e.IndexOf("firstname", StringComparison.OrdinalIgnoreCase) >= 0),
                "expected an error message about the firstname when firstname is missing");
        }
        else
        {
            errors.Should().NotContain(e => e != null &&
                (e.IndexOf("first", StringComparison.OrdinalIgnoreCase) >= 0
                 || e.IndexOf("firstname", StringComparison.OrdinalIgnoreCase) >= 0),
                "did not expect firstname validation when firstname is provided");
        }

        if (lastMissing)
        {
            errors.Should().Contain(e => e != null &&
                (e.IndexOf("last", StringComparison.OrdinalIgnoreCase) >= 0
                 || e.IndexOf("lastname", StringComparison.OrdinalIgnoreCase) >= 0),
                "expected an error message about the lastname when lastname is missing");
        }
        else
        {
            errors.Should().NotContain(e => e != null &&
                (e.IndexOf("last", StringComparison.OrdinalIgnoreCase) >= 0
                 || e.IndexOf("lastname", StringComparison.OrdinalIgnoreCase) >= 0),
                "did not expect lastname validation when lastname is provided");
        }
    }
}