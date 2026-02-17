using Arancia.Test.API.Clients;
using Arancia.Test.API.Helpers;
using FluentAssertions;
using RestSharp;
using System.Net;
using System.Text.Json;
using Xunit.Abstractions;

public class DeleteBookingTests : TestBase
{
    public DeleteBookingTests(ITestOutputHelper output) => InitTestBase(output);
    [Fact(DisplayName = "API-14 - Delete booking with valid authentication")]
    public async Task DeleteBooking_WithAuth_RemovesBooking()
    {
        // Preconditions
        Output.Should().NotBeNull();

        string? token = null;
        var auth = new AutomationTestingAuthClient();

        try
        {
            // Arrange - create booking on automation API
            var booking = CreateRandomBooking();
            var bookingClient = new BookingClient(ApiClientFactory.Create(Settings.AutomationTestingApiBase));
            var createResp = await bookingClient.CreateBookingAsync(booking);
            createResp.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
            createResp.Content.Should().NotBeNullOrWhiteSpace();

            using var createdDoc = JsonDocument.Parse(createResp.Content!);
            createdDoc.RootElement.TryGetProperty("bookingid", out var idEl).Should().BeTrue();
            var bookingId = idEl.GetInt32();
            Output.WriteLine($"Created booking id: {bookingId}");

            // Auth - get token from AutomationTestingAuthClient
            token = await auth.GetTokenAsync("admin", "password");
            token.Should().NotBeNullOrWhiteSpace();
            Output.WriteLine($"Token: {token}");

            // Act - DELETE /booking/{id} with auth (empty body)
            var deleteResp = await bookingClient.DeleteBookingAsync(bookingId, token);
            BookingTestHelper.LogRequestResponse(Output, $"DELETE /booking/{bookingId}", deleteResp);

            // Assert delete success (accept 200/201)
            ((int)deleteResp.StatusCode).Should().BeOneOf(200, 201);

            // Verify removal: try GET /booking/{id} with auth, accept 404 or 401/403 if protected
            var helper = new BookingApiHelper(ApiClientFactory.Create(Settings.AutomationTestingApiBase));
            var getResp = await helper.GetBookingRawAsync(bookingId, token);
            BookingTestHelper.LogRequestResponse(Output, $"GET /booking/{bookingId} after delete", getResp);

            if (getResp.StatusCode == HttpStatusCode.NotFound)
            {
                getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
            }
            else if (getResp.StatusCode == HttpStatusCode.Unauthorized || getResp.StatusCode == HttpStatusCode.Forbidden)
            {
                Output.WriteLine($"GET after delete requires auth (status {(int)getResp.StatusCode}).");
            }
            else
            {
                throw new Xunit.Sdk.XunitException(
                    $"Unexpected status {(int)getResp.StatusCode} after delete. Response: {getResp.Content}");
            }
        }
        finally
        {
            // Teardown: destroy the token if it was successfully obtained
            if (!string.IsNullOrWhiteSpace(token))
            {
                try
                {
                    await auth.DestroyTokenAsync(token!);
                    Output.WriteLine("Admin token destroyed successfully after booking delete test.");
                }
                catch (Exception ex)
                {
                    Output.WriteLine($"Failed to destroy admin token after booking delete test: {ex.Message}");
                }
            }
        }
    }

    [Fact(DisplayName = "API-15 - Block booking deletion without authentication")]
    public async Task DeleteBooking_WithoutAuth_IsRejected()
    {
        // Preconditions
        Output.Should().NotBeNull();
        // Arrange - create booking on automation API
        var booking = CreateRandomBooking();
        var bookingClient = new BookingClient(ApiClientFactory.Create(Settings.AutomationTestingApiBase));
        var createResp = await bookingClient.CreateBookingAsync(booking);
        createResp.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        createResp.Content.Should().NotBeNullOrWhiteSpace();

        using var createdDoc = JsonDocument.Parse(createResp.Content!);
        createdDoc.RootElement.TryGetProperty("bookingid", out var idEl).Should().BeTrue();
        var bookingId = idEl.GetInt32();
        Output.WriteLine($"Created booking id: {bookingId}");

        // Act - DELETE /booking/{id} WITHOUT auth (no Cookie header)
        var client = ApiClientFactory.Create(Settings.AutomationTestingApiBase);
        var req = new RestRequest($"booking/{bookingId}", Method.Delete)
            .AddHeader("Accept", "application/json");
        var deleteResp = await client.ExecuteAsync(req);

        // Log
        BookingTestHelper.LogRequestResponse(Output, $"DELETE /booking/{bookingId} (no auth)", deleteResp);

        // Assert - expect 401/403 or other 4xx (deny). Fail if 200.
        if (deleteResp.StatusCode == HttpStatusCode.Unauthorized || deleteResp.StatusCode == HttpStatusCode.Forbidden)
        {
            deleteResp.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
        }
        else if ((int)deleteResp.StatusCode >= 400 && (int)deleteResp.StatusCode < 500)
        {
            ((int)deleteResp.StatusCode).Should().BeInRange(400, 499);
        }
        else if (deleteResp.StatusCode == HttpStatusCode.OK)
        {
            throw new Xunit.Sdk.XunitException($"DELETE without auth unexpectedly succeeded (200). Response: {deleteResp.Content}");
        }
        else
        {
            throw new Xunit.Sdk.XunitException($"Unexpected status {(int)deleteResp.StatusCode} for unauthenticated delete. Response: {deleteResp.Content}");
        }

        // Verify booking still exists: GET may require auth; if 200 validate content, if 401/403 accept protected
        var helper = new BookingApiHelper(ApiClientFactory.Create(Settings.AutomationTestingApiBase));
        var getResp = await helper.GetBookingRawAsync(bookingId);

        BookingTestHelper.LogRequestResponse(Output, $"GET /booking/{bookingId} after failed delete", getResp);

        if (getResp.StatusCode == HttpStatusCode.OK)
        {
            getResp.Content.Should().NotBeNullOrWhiteSpace();
            using var getDoc = JsonDocument.Parse(getResp.Content!);
            var root = getDoc.RootElement;
            root.GetProperty("firstname").GetString().Should().Be(booking.firstname);
            root.GetProperty("lastname").GetString().Should().Be(booking.lastname);
        }
        else if (getResp.StatusCode == HttpStatusCode.Unauthorized || getResp.StatusCode == HttpStatusCode.Forbidden)
        {
            Output.WriteLine($"GET /booking/{bookingId} requires auth (status {(int)getResp.StatusCode}). Cannot verify content, but delete was rejected.");
        }
        else
        {
            throw new Xunit.Sdk.XunitException($"Unexpected status {(int)getResp.StatusCode} when verifying booking after failed delete. Response: {getResp.Content}");
        }
    }
}
