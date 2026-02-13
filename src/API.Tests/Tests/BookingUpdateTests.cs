using System.Net;
using System.Text.Json;
using FluentAssertions;
using RestSharp;
using Xunit;
using Xunit.Abstractions;

public class BookingUpdateTests : TestBase
{
    private readonly ITestOutputHelper _output;

    public BookingUpdateTests(ITestOutputHelper output) => _output = output;

    // ========= API-10 =========
    // API-10a — PUT with auth (only PUT response, no GET)
    [Fact(DisplayName = "API-10a - PUT booking with valid auth (no GET verification)")]
    public async Task UpdateBooking_Put_WithAuth_OnlyPutResponse()
    {
        // Arrange
        var bookingClient = new BookingClient();
        var original = CreateRandomBooking();
        var id = await BookingTestHelper.CreateBookingAndGetIdAsync(bookingClient, original);
        _output.WriteLine($"[CREATE] booking id: {id}");

        var token = await new AuthClient().GetTokenAsync();
        var updated = CreateRandomBooking();

        var client = ApiClientFactory.Create(Settings.ApiBaseUrl);
        var req = new RestRequest($"booking/{id}", Method.Put)
            .AddHeader("Accept", "application/json")
            .AddHeader("Content-Type", "application/json")
            .AddHeader("Cookie", $"token={token}")
            .AddJsonBody(updated);

        // Act
        var resp = await client.ExecuteAsync(req);

        _output.WriteLine($"[PUT] Status: {(int)resp.StatusCode} - {resp.StatusCode}");
        _output.WriteLine($"[PUT] Body  : {resp.Content}");

        // Assert: only validate PUT response
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Should().NotBeNullOrEmpty();

        using var doc = JsonDocument.Parse(resp.Content!);
        var root = doc.RootElement;

        root.GetProperty("firstname").GetString().Should().Be(updated.firstname);
        root.GetProperty("lastname").GetString().Should().Be(updated.lastname);
        root.GetProperty("totalprice").GetInt32().Should().Be(updated.totalprice);
        root.GetProperty("depositpaid").GetBoolean().Should().Be(updated.depositpaid);

        var dates = root.GetProperty("bookingdates");
        dates.GetProperty("checkin").GetString().Should().Be(updated.bookingdates!.checkin);
        dates.GetProperty("checkout").GetString().Should().Be(updated.bookingdates!.checkout);

        if (updated.additionalneeds is not null)
            root.GetProperty("additionalneeds").GetString().Should().Be(updated.additionalneeds);
    }

    // API-10b — GET after PUT (known issue 418) – skipped
    [Fact(
        DisplayName = "API-10b - GET booking after PUT (known 418 issue)",
        Skip = "Known flaky issue: GET /booking/{id} sometimes returns 418 after PUT in demo environment")]
    public async Task UpdateBooking_GetAfterPut_KnownIssue()
    {
        // Same flow as 10a but including GET and known-issue handling (kept for documentation)

        var bookingClient = new BookingClient();
        var original = CreateRandomBooking();
        var id = await BookingTestHelper.CreateBookingAndGetIdAsync(bookingClient, original);

        var token = await new AuthClient().GetTokenAsync();
        var updated = CreateRandomBooking();

        var client = ApiClientFactory.Create(Settings.ApiBaseUrl);
        var req = new RestRequest($"booking/{id}", Method.Put)
            .AddHeader("Accept", "application/json")
            .AddHeader("Content-Type", "application/json")
            .AddHeader("Cookie", $"token={token}")
            .AddJsonBody(updated);

        var resp = await client.ExecuteAsync(req);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Should().NotBeNullOrEmpty();

        var getResp = await BookingTestHelper.GetBookingByIdWithRetryAsync(id);
        if (getResp.StatusCode == (HttpStatusCode)418)
        {
            throw new Xunit.Sdk.XunitException(
                $"Known issue: GET /booking/{id} returned 418 I'm a Teapot after successful PUT. " +
                $"Response: {getResp.Content}");
        }

        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ========= API-11 =========
    // API-11a — PUT without auth (only PUT response, no GET)
    [Fact(DisplayName = "API-11a - PUT booking without auth (blocked, no GET verification)")]
    public async Task UpdateBooking_Put_WithoutAuth_OnlyPutResponse()
    {
        // Arrange
        var bookingClient = new BookingClient();
        var original = CreateRandomBooking();
        var id = await BookingTestHelper.CreateBookingAndGetIdAsync(bookingClient, original);
        _output.WriteLine($"[CREATE] booking id: {id}");

        var updated = CreateRandomBooking();

        var client = ApiClientFactory.Create(Settings.ApiBaseUrl);
        var req = new RestRequest($"booking/{id}", Method.Put)
            .AddHeader("Accept", "application/json")
            .AddHeader("Content-Type", "application/json")
            // no auth on purpose
            .AddJsonBody(updated);

        // Act
        var resp = await client.ExecuteAsync(req);

        _output.WriteLine($"[PUT no auth] Status: {(int)resp.StatusCode} - {resp.StatusCode}");
        _output.WriteLine($"[PUT no auth] Body  : {resp.Content}");

        // Assert: only check that update is blocked
        resp.StatusCode
            .Should()
            .BeOneOf(new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden });
    }

    // API-11b — GET after forbidden PUT (known 418 issue) – skipped
    [Fact(
        DisplayName = "API-11b - GET booking after forbidden PUT (known 418 issue)",
        Skip = "Known flaky issue: GET /booking/{id} sometimes returns 418 after forbidden PUT in demo environment")]
    public async Task UpdateBooking_GetAfterForbiddenPut_KnownIssue()
    {
        var bookingClient = new BookingClient();
        var original = CreateRandomBooking();
        var id = await BookingTestHelper.CreateBookingAndGetIdAsync(bookingClient, original);

        var updated = CreateRandomBooking();

        var client = ApiClientFactory.Create(Settings.ApiBaseUrl);
        var req = new RestRequest($"booking/{id}", Method.Put)
            .AddHeader("Accept", "application/json")
            .AddHeader("Content-Type", "application/json")
            .AddJsonBody(updated);

        var resp = await client.ExecuteAsync(req);
        resp.StatusCode
            .Should()
            .BeOneOf(new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden });

        var getResp = await BookingTestHelper.GetBookingByIdWithRetryAsync(id);
        if (getResp.StatusCode == (HttpStatusCode)418)
        {
            throw new Xunit.Sdk.XunitException(
                $"Known issue: GET /booking/{id} returned 418 I'm a Teapot after forbidden PUT. " +
                $"Response: {getResp.Content}");
        }

        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ========= API-12 =========
    // API-12a — PATCH with auth (only PATCH response, no GET)
    [Fact(DisplayName = "API-12a - PATCH booking with auth (no GET verification)")]
    public async Task PartialUpdateBooking_Patch_WithAuth_OnlyPatchResponse()
    {
        // Arrange
        var bookingClient = new BookingClient();
        var original = CreateRandomBooking();
        var id = await BookingTestHelper.CreateBookingAndGetIdAsync(bookingClient, original);
        _output.WriteLine($"[CREATE] booking id: {id}");

        var token = await new AuthClient().GetTokenAsync();
        var newFirstName = "UpdatedName";

        var client = ApiClientFactory.Create(Settings.ApiBaseUrl);
        var req = new RestRequest($"booking/{id}", Method.Patch)
            .AddHeader("Accept", "application/json")
            .AddHeader("Content-Type", "application/json")
            .AddHeader("Cookie", $"token={token}")
            .AddJsonBody(new { firstname = newFirstName });

        // Act
        var resp = await client.ExecuteAsync(req);

        _output.WriteLine($"[PATCH] Status: {(int)resp.StatusCode} - {resp.StatusCode}");
        _output.WriteLine($"[PATCH] Body  : {resp.Content}");

        // Assert: only validate PATCH response
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Should().NotBeNullOrEmpty();

        using var doc = JsonDocument.Parse(resp.Content!);
        var root = doc.RootElement;

        root.GetProperty("firstname").GetString().Should().Be(newFirstName);
        root.GetProperty("lastname").GetString().Should().Be(original.lastname);
        root.GetProperty("totalprice").GetInt32().Should().Be(original.totalprice);
        root.GetProperty("depositpaid").GetBoolean().Should().Be(original.depositpaid);

        var dates = root.GetProperty("bookingdates");
        dates.GetProperty("checkin").GetString().Should().Be(original.bookingdates!.checkin);
        dates.GetProperty("checkout").GetString().Should().Be(original.bookingdates!.checkout);

        if (original.additionalneeds is not null)
            root.GetProperty("additionalneeds").GetString().Should().Be(original.additionalneeds);
    }

    // API-12b — GET after PATCH (known 418 issue) – skipped
    [Fact(
        DisplayName = "API-12b - GET booking after PATCH (known 418 issue)",
        Skip = "Known flaky issue: GET /booking/{id} sometimes returns 418 after PATCH in demo environment")]
    public async Task PartialUpdateBooking_GetAfterPatch_KnownIssue()
    {
        var bookingClient = new BookingClient();
        var original = CreateRandomBooking();
        var id = await BookingTestHelper.CreateBookingAndGetIdAsync(bookingClient, original);

        var token = await new AuthClient().GetTokenAsync();
        var newFirstName = "UpdatedName";

        var client = ApiClientFactory.Create(Settings.ApiBaseUrl);
        var req = new RestRequest($"booking/{id}", Method.Patch)
            .AddHeader("Accept", "application/json")
            .AddHeader("Content-Type", "application/json")
            .AddHeader("Cookie", $"token={token}")
            .AddJsonBody(new { firstname = newFirstName });

        var resp = await client.ExecuteAsync(req);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResp = await BookingTestHelper.GetBookingByIdWithRetryAsync(id);
        if (getResp.StatusCode == (HttpStatusCode)418)
        {
            throw new Xunit.Sdk.XunitException(
                $"Known issue: GET /booking/{id} returned 418 I'm a Teapot after PATCH. " +
                $"Response: {getResp.Content}");
        }

        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "API-13 - Update booking with invalid ID")]
    public async Task UpdateBooking_Put_WithInvalidId_ReturnsClientError()
    {
        // Arrange
        var token = await new AuthClient().GetTokenAsync();
        var updated = CreateRandomBooking();

        var client = ApiClientFactory.Create(Settings.ApiBaseUrl);
        var req = new RestRequest("booking/abc", Method.Put)
            .AddHeader("Accept", "application/json")
            .AddHeader("Content-Type", "application/json")
            .AddHeader("Cookie", $"token={token}")
            .AddJsonBody(updated);

        // Act
        var resp = await client.ExecuteAsync(req);

        _output.WriteLine($"[PUT invalid id] Status: {(int)resp.StatusCode} - {resp.StatusCode}");
        _output.WriteLine($"[PUT invalid id] Body  : {resp.Content}");

        // Assert: accept 400/404/405 as client error for invalid ID
        resp.StatusCode
            .Should()
            .BeOneOf(
                new[] { HttpStatusCode.BadRequest, HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed },
                $"Expected client error for invalid id 'abc'. Actual: {(int)resp.StatusCode} - {resp.StatusCode}, body: {resp.Content}");
    }
}
