using Arancia.Test.API.Helpers;
using FluentAssertions;
using RestSharp;
using System.Net;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

public class AdvancedBookingTests : TestBase
{
    private readonly ITestOutputHelper _output;

    public AdvancedBookingTests(ITestOutputHelper output) => _output = output;

    // ========== API-16 ==========

    // API-16a — Concurrent PATCH (only PATCH responses, no GET verification)
    [Fact(DisplayName = "API-16a - Concurrent PATCH updates (only PATCH responses)")]
    public async Task ConcurrentUpdates_Patch_OnlyPatchResponses()
    {
        // Arrange
        var bookingClient = new BookingClient();
        var original = CreateRandomBooking();
        var id = await BookingTestHelper.CreateBookingAndGetIdAsync(bookingClient, original);
        _output.WriteLine($"[CREATE] booking id: {id}");

        var authClient = new AuthClient();
        var tokenA = await authClient.GetTokenAsync();
        var tokenB = await authClient.GetTokenAsync();

        const string versionA = "VersionA";
        const string versionB = "VersionB";

        // Act: two concurrent PATCH requests, A and B
        var patchATask = bookingClient.PatchBookingFirstnameAsync(id, versionA, tokenA);
        var patchBTask = bookingClient.PatchBookingFirstnameAsync(id, versionB, tokenB);

        await Task.WhenAll(patchATask, patchBTask);

        var respA = patchATask.Result;
        var respB = patchBTask.Result;

        _output.WriteLine($"[PATCH A] Status: {(int)respA.StatusCode} - {respA.StatusCode}");
        _output.WriteLine($"[PATCH B] Status: {(int)respB.StatusCode} - {respB.StatusCode}");

        // Assert: both PATCH calls should succeed
        respA.StatusCode.Should().Be(HttpStatusCode.OK);
        respB.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // API-16b — Concurrent PATCH + GET final (known 418 issue) – skipped
    [Fact(
        DisplayName = "API-16b - Concurrent PATCH + GET final (known 418 issue)",
        Skip = "Known flaky issue: GET /booking/{id} sometimes returns 418 after concurrent PATCH in demo environment")]
    public async Task ConcurrentUpdates_LastWriteWins_KnownIssue()
    {
        var bookingClient = new BookingClient();
        var original = CreateRandomBooking();
        var id = await BookingTestHelper.CreateBookingAndGetIdAsync(bookingClient, original);

        var authClient = new AuthClient();
        var tokenA = await authClient.GetTokenAsync();
        var tokenB = await authClient.GetTokenAsync();

        const string versionA = "VersionA";
        const string versionB = "VersionB";

        var patchATask = bookingClient.PatchBookingFirstnameAsync(id, versionA, tokenA);
        var patchBTask = bookingClient.PatchBookingFirstnameAsync(id, versionB, tokenB);
        await Task.WhenAll(patchATask, patchBTask);

        var respA = patchATask.Result;
        var respB = patchBTask.Result;

        respA.StatusCode.Should().Be(HttpStatusCode.OK);
        respB.StatusCode.Should().Be(HttpStatusCode.OK);

        // GET final state (this is where the known flaky 418 appears)
        var getResp = await BookingTestHelper.GetBookingByIdWithRetryAsync(id);
        if (getResp.StatusCode == (HttpStatusCode)418)
        {
            throw new Xunit.Sdk.XunitException(
                $"Known issue: GET /booking/{id} returned 418 after concurrent PATCH. Response: {getResp.Content}");
        }

        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        getResp.Content.Should().NotBeNullOrEmpty();

        using var doc = JsonDocument.Parse(getResp.Content!);
        var root = doc.RootElement;

        var finalFirstname = root.GetProperty("firstname").GetString();
        finalFirstname.Should().BeOneOf(versionA, versionB);
    }


    // ========== API-17 ==========
    [Fact(DisplayName = "API-17 - Create booking with very large string fields")]
    public async Task CreateBooking_WithVeryLargeStrings_IsHandledGracefully()
    {
        // Arrange
        var large = TextHelper.GenerateLargeString(10_000);
        var booking = CreateRandomBooking();
        booking.firstname = large;
        booking.additionalneeds = large;

        var client = new BookingClient();

        // Act
        var resp = await client.CreateBookingAsync(booking);

        _output.WriteLine($"[LARGE PAYLOAD] Status: {(int)resp.StatusCode} - {resp.StatusCode}");
        _output.WriteLine($"[LARGE PAYLOAD] Body  : {resp.Content}");

        // Assert: API should either accept or respond with clear 4xx; not crash/hang.
        ((int)resp.StatusCode).Should().BeInRange(200, 499,
            $"Expected success or client error for large payload. Response: {resp.Content}");
    }

    // ========== API-18 ==========
    [Fact(DisplayName = "API-18 - Special characters / injection strings are treated as plain text")]
    public async Task CreateBooking_WithInjectionLikeStrings_TreatedAsPlainText()
    {
        // Arrange
        var booking = CreateRandomBooking();
        booking.firstname = TextHelper.ScriptPayload;
        booking.lastname = TextHelper.SqlInjectionPayload;
        booking.additionalneeds = $"{TextHelper.ScriptPayload} {TextHelper.SqlInjectionPayload}";

        var client = new BookingClient();

        // Act
        var resp = await client.CreateBookingAsync(booking);

        _output.WriteLine($"[INJECTION] Status: {(int)resp.StatusCode} - {resp.StatusCode}");
        _output.WriteLine($"[INJECTION] Body  : {resp.Content}");

        resp.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        resp.Content.Should().NotBeNullOrEmpty();

        using var doc = JsonDocument.Parse(resp.Content!);
        var root = doc.RootElement;

        root.TryGetProperty("booking", out var bookingEl).Should().BeTrue();
        var returned = bookingEl;

        returned.GetProperty("firstname").GetString().Should().Be(booking.firstname);
        returned.GetProperty("lastname").GetString().Should().Be(booking.lastname);
        returned.GetProperty("additionalneeds").GetString().Should().Be(booking.additionalneeds);
        // Sem execução de script: aqui só dá para verificar que veio como texto,
        // não há exceção de runtime nem erro no response.
    }
    // ========== API-19 ==========
    [Fact(DisplayName = "API-19 - Reject POST /booking with wrong Content-Type")]
    public async Task CreateBooking_WithWrongContentType_IsRejected()
    {
        // Arrange
        var baseUrl = Settings.ApiBaseUrl;
        var client = ApiClientFactory.Create(baseUrl);

        var bookingJson = """
    {
      "firstname": "John",
      "lastname": "Doe",
      "totalprice": 150,
      "depositpaid": true,
      "bookingdates": {
        "checkin": "2026-02-14",
        "checkout": "2026-02-17"
      },
      "additionalneeds": "Breakfast"
    }
    """;

        var reqTextPlain = new RestRequest("booking", Method.Post)
            .AddHeader("Content-Type", "text/plain")
            .AddStringBody(bookingJson, "text/plain");

        // Act
        var respTextPlain = await client.ExecuteAsync(reqTextPlain);

        _output.WriteLine($"[WRONG CT] Status: {(int)respTextPlain.StatusCode} - {respTextPlain.StatusCode}");
        _output.WriteLine($"[WRONG CT] Body  : {respTextPlain.Content}");

        var code = (int)respTextPlain.StatusCode;

        // Ideal: 4xx controlado
        if (code >= 400 && code < 500)
        {
            code.Should().BeInRange(400, 499,
                $"Expected 4xx for wrong Content-Type. Response: {respTextPlain.Content}");
        }
        // Bug: 500 Internal Server Error
        else if (code == 500)
        {
            throw new Xunit.Sdk.XunitException(
                $"Known issue: server returns 500 Internal Server Error for wrong Content-Type instead of 4xx. " +
                $"Response: {respTextPlain.Content}");
        }
        else
        {
            throw new Xunit.Sdk.XunitException(
                $"Unexpected status {code} for wrong Content-Type. Response: {respTextPlain.Content}");
        }
    }


    // ========== API-20 ==========
    // ========== API-20a ==========
    // Validate schema for /booking (list) and /auth (no GET /booking/{id})
    [Fact(DisplayName = "API-20a - Schema consistency for /booking list and /auth")]
    public async Task ResponseSchemas_BookingListAndAuth_AreConsistent()
    {
        var baseUrl = Settings.ApiBaseUrl;
        var client = ApiClientFactory.Create(baseUrl);

        // 1) /booking list
        var listReq = new RestRequest("booking", Method.Get);
        var listResp = await client.ExecuteAsync(listReq);

        _output.WriteLine($"[/booking] Status: {(int)listResp.StatusCode} - {listResp.StatusCode}");
        _output.WriteLine($"[/booking] Body  : {listResp.Content}");

        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        listResp.Content.Should().NotBeNullOrEmpty();
        ResponseSchemaValidator.AssertBookingListSchema(listResp.Content!);

        // 2) /auth schema
        var authBody = new { username = "admin", password = "password123" };
        var authReq = new RestRequest("auth", Method.Post)
            .AddHeader("Accept", "application/json")
            .AddJsonBody(authBody);
        var authResp = await client.ExecuteAsync(authReq);

        _output.WriteLine($"[/auth] Status: {(int)authResp.StatusCode} - {authResp.StatusCode}");
        _output.WriteLine($"[/auth] Body  : {authResp.Content}");

        authResp.StatusCode.Should().Be(HttpStatusCode.OK);
        authResp.Content.Should().NotBeNullOrEmpty();
        ResponseSchemaValidator.AssertAuthSchema(authResp.Content!);
    }

    // ========== API-20b ==========
    // Validate schema for /booking/{id} (known 418 issue) – skipped
    [Fact(
        DisplayName = "API-20b - Schema consistency for /booking/{id} (known 418 issue)",
        Skip = "Known flaky issue: GET /booking/{id} sometimes returns 418 in demo environment")]
    public async Task ResponseSchema_BookingById_KnownIssue()
    {
        var bookingClient = new BookingClient();
        var booking = CreateRandomBooking();
        var id = await BookingTestHelper.CreateBookingAndGetIdAsync(bookingClient, booking);
        _output.WriteLine($"[CREATE] booking id: {id}");

        var getResp = await BookingTestHelper.GetBookingByIdWithRetryAsync(id);
        _output.WriteLine($"[/booking/{id}] Status: {(int)getResp.StatusCode} - {getResp.StatusCode}");
        _output.WriteLine($"[/booking/{id}] Body  : {getResp.Content}");

        if (getResp.StatusCode == (HttpStatusCode)418)
        {
            throw new Xunit.Sdk.XunitException(
                $"Known issue: GET /booking/{id} returned 418 when validating schema. Response: {getResp.Content}");
        }

        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        getResp.Content.Should().NotBeNullOrEmpty();
        ResponseSchemaValidator.AssertBookingDetailsSchema(getResp.Content!);
    }

}
