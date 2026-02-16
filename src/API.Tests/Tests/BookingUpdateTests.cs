using Arancia.Test.API.Clients;
using Arancia.Test.API.Helpers;
using FluentAssertions;
using RestSharp;
using System.Net;
using System.Text.Json;
using Xunit.Abstractions;

public class BookingUpdateTests : TestBase
{
    public BookingUpdateTests(ITestOutputHelper output) => InitTestBase(output);

    [Fact(DisplayName = "API-10 - PUT booking with valid auth")]
    public async Task UpdateBooking_Put_WithAuth_AndGetBooking()
    {
        // Preconditions
        Output.Should().NotBeNull();

        // Arrange — create booking on the automation API (ensure same backend for create/put/get)
        var booking = CreateRandomBooking();
        var bookingClient = new BookingClient(ApiClientFactory.Create(Settings.AutomationTestingApiBase));
        var createResp = await bookingClient.CreateBookingAsync(booking);

        createResp.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        createResp.Content.Should().NotBeNullOrWhiteSpace();

        using var createdDoc = JsonDocument.Parse(createResp.Content!);
        createdDoc.RootElement.TryGetProperty("bookingid", out var idEl).Should().BeTrue();
        var bookingId = idEl.GetInt32();
        Output.WriteLine($"Created booking id: {bookingId}");

        // Auth — obtain token from AutomationTestingAuthClient (automationintesting.online)
        var automationAuth = new AutomationTestingAuthClient();
        var token = await automationAuth.GetTokenAsync("admin", "password");
        token.Should().NotBeNullOrWhiteSpace();
        Output.WriteLine($"Auth token: {token}");

        // Prepare updated payload (full replacement per PUT)
        var updatedPayload = new
        {
            roomid = 13,
            firstname = "James",
            lastname = "Dean",
            depositpaid = true,
            bookingdates = new { checkin = "2026-02-01", checkout = "2026-02-05" }
        };

        var helper = new BookingApiHelper(ApiClientFactory.Create(Settings.AutomationTestingApiBase));

        // Act — PUT /booking/{id} (automation API base)
        var putResp = await helper.PutBookingAsync(bookingId, updatedPayload, token);
        BookingTestHelper.LogRequestResponse(Output, $"PUT /booking/{bookingId}", putResp);

        // Assert PUT response
        putResp.StatusCode.Should().Be(HttpStatusCode.OK);
        putResp.Content.Should().NotBeNullOrWhiteSpace();
        using var putDoc = JsonDocument.Parse(putResp.Content!);
        putDoc.RootElement.TryGetProperty("success", out var successEl).Should().BeTrue();
        successEl.GetBoolean().Should().BeTrue();

        // Verify persistence via GET /booking/{id} (automation API base, same token)
        var getResp = await helper.GetBookingRawAsync(bookingId, token);
        BookingTestHelper.LogRequestResponse(Output, $"GET /booking/{bookingId}", getResp);

        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        getResp.Content.Should().NotBeNullOrWhiteSpace();

        using var getDoc = JsonDocument.Parse(getResp.Content!);
        var root = getDoc.RootElement;

        root.GetProperty("firstname").GetString().Should().Be((string)updatedPayload.firstname);
        root.GetProperty("lastname").GetString().Should().Be((string)updatedPayload.lastname);
        root.GetProperty("depositpaid").GetBoolean().Should().Be((bool)updatedPayload.depositpaid);

        var returnedDates = root.GetProperty("bookingdates");
        returnedDates.GetProperty("checkin").GetString().Should().Be((string)updatedPayload.bookingdates.checkin);
        returnedDates.GetProperty("checkout").GetString().Should().Be((string)updatedPayload.bookingdates.checkout);
    }


    [Fact(DisplayName = "API-11 - PUT booking without auth")]
    public async Task UpdateBooking_Put_WithoutAuth_OnlyPutResponse()
    {
        // Arrange — create booking on automation API
        var booking = CreateRandomBooking();
        var bookingClient = new BookingClient(ApiClientFactory.Create(Settings.AutomationTestingApiBase));
        var createResp = await bookingClient.CreateBookingAsync(booking);
        createResp.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        createResp.Content.Should().NotBeNullOrWhiteSpace();
        using var createdDoc = JsonDocument.Parse(createResp.Content!);
        createdDoc.RootElement.TryGetProperty("bookingid", out var idEl).Should().BeTrue();
        var bookingId = idEl.GetInt32();
        Output.WriteLine($"Created booking id: {bookingId}");

        // Prepare updated payload
        var updatedPayload = new
        {
            roomid = 13,
            firstname = "NoAuth",
            lastname = "Attempt",
            depositpaid = false,
            bookingdates = new { checkin = "2026-03-01", checkout = "2026-03-05" }
        };
        var json = JsonSerializer.Serialize(updatedPayload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        // Act — PUT /booking/{id} WITHOUT auth (no Cookie header)
        var client = ApiClientFactory.Create(Settings.AutomationTestingApiBase);
        var req = new RestRequest($"booking/{bookingId}", Method.Put)
            .AddHeader("Referer", "")
            .AddHeader("Content-Type", "application/json")
            .AddStringBody(json, "application/json");

        var putResp = await client.ExecuteAsync(req);

        // Log
        BookingTestHelper.LogRequestResponse(Output, $"PUT /booking/{bookingId} (no auth)", putResp);

        // Assert — accept 401 (Authentication required) or 403 (Failed to update booking)
        if (putResp.StatusCode == HttpStatusCode.Unauthorized)
        {
            putResp.Content.Should().NotBeNullOrWhiteSpace();
            using var errDoc = JsonDocument.Parse(putResp.Content!);
            errDoc.RootElement.TryGetProperty("error", out var errEl).Should().BeTrue();
            errEl.GetString().Should().Be("Authentication required");
        }
        else if (putResp.StatusCode == HttpStatusCode.Forbidden)
        {
            putResp.Content.Should().NotBeNullOrWhiteSpace();
            using var errDoc = JsonDocument.Parse(putResp.Content!);
            errDoc.RootElement.TryGetProperty("error", out var errEl).Should().BeTrue();
            errEl.GetString().Should().Be("Failed to update booking");
        }
        else if ((int)putResp.StatusCode >= 400 && (int)putResp.StatusCode < 500)
        {
            // Other client rejection is acceptable
            ((int)putResp.StatusCode).Should().BeInRange(400, 499);
        }
        else
        {
            throw new Xunit.Sdk.XunitException($"Unexpected status {(int)putResp.StatusCode} for PUT without auth. Response: {putResp.Content}");
        }

        // Verify booking unchanged via GET — accept 200 (verify content) or 401/403 (resource protected)
        var helper = new BookingApiHelper(ApiClientFactory.Create(Settings.AutomationTestingApiBase));
        var getResp = await helper.GetBookingRawAsync(bookingId);

        BookingTestHelper.LogRequestResponse(Output, $"GET /booking/{bookingId}", getResp);

        if (getResp.StatusCode == HttpStatusCode.OK)
        {
            getResp.Content.Should().NotBeNullOrWhiteSpace();
            using var getDoc = JsonDocument.Parse(getResp.Content!);
            var root = getDoc.RootElement;

            root.GetProperty("firstname").GetString().Should().Be(booking.firstname);
            root.GetProperty("lastname").GetString().Should().Be(booking.lastname);
            root.GetProperty("depositpaid").GetBoolean().Should().Be(booking.depositpaid);

            var returnedDates = root.GetProperty("bookingdates");
            returnedDates.GetProperty("checkin").GetString().Should().Be(booking.bookingdates!.checkin);
            returnedDates.GetProperty("checkout").GetString().Should().Be(booking.bookingdates!.checkout);
        }
        else if (getResp.StatusCode == HttpStatusCode.Unauthorized || getResp.StatusCode == HttpStatusCode.Forbidden)
        {
            Output.WriteLine($"GET /booking/{bookingId} requires auth (status {(int)getResp.StatusCode}). Skipping content verification.");
        }
        else
        {
            throw new Xunit.Sdk.XunitException($"Unexpected status {(int)getResp.StatusCode} when verifying booking. Response: {getResp.Content}");
        }

    }

    [Fact(DisplayName = "API-13 - Update booking with invalid ID")]
    public async Task UpdateBooking_Put_WithInvalidId_ReturnsClientError()
    {
        // Preconditions
        Output.Should().NotBeNull();
        // Arrange - obtain auth token for Automation API
        var automationAuth = new AutomationTestingAuthClient();
        var token = await automationAuth.GetTokenAsync("admin", "password");
        token.Should().NotBeNullOrWhiteSpace();

        // Prepare payload
        var payload = new
        {
            roomid = 13,
            firstname = "Invalid",
            lastname = "Id",
            depositpaid = false,
            bookingdates = new { checkin = "2026-03-01", checkout = "2026-03-05" }
        };
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        // Act - call PUT /booking/{invalidId} using a non-numeric id segment
        var client = ApiClientFactory.Create(Settings.AutomationTestingApiBase);
        var req = new RestRequest("booking/abc", Method.Put) // invalid id "abc"
            .AddHeader("Referer", "")
            .AddHeader("Content-Type", "application/json")
            .AddHeader("Cookie", $"token={token}")
            .AddStringBody(json, "application/json");

        var resp = await client.ExecuteAsync(req);

        // Log
        BookingTestHelper.LogRequestResponse(Output, "PUT /booking/abc", resp);

        // Assert: expect client error (4xx). If server error (5xx) -> fail for triage.
        var code = (int)resp.StatusCode;
        if (code >= 400 && code < 500)
        {
            code.Should().BeInRange(400, 499);
        }
        else if (code >= 500 && code < 600)
        {
            throw new Xunit.Sdk.XunitException($"Server error for PUT with invalid id. Status: {code}. Body: {resp.Content}");
        }
        else
        {
            throw new Xunit.Sdk.XunitException($"Unexpected status {code} for PUT with invalid id. Body: {resp.Content}");
        }

    }
}
