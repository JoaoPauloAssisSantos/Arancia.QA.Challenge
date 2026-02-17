using Arancia.Test.API.Clients;
using Arancia.Test.API.Helpers;
using FluentAssertions;
using RestSharp;
using System.Net;
using System.Text.Json;
using Xunit.Abstractions;

public class AdvancedBookingTests : TestBase
{
    public AdvancedBookingTests(ITestOutputHelper output) => InitTestBase(output);

    [Theory(DisplayName = "API-17 - Create booking with invalid/missing Content-Type")]
    [InlineData("text/plain")]       // case -> use RestSharp with wrong content-type
    [InlineData(null)]               // case -> use HttpClient and remove Content-Type header
    public async Task CreateBooking_InvalidOrMissingContentType_IsRejected(string? contentType)
    {
        // Preconditions
        Output.Should().NotBeNull();

        // Arrange - normal booking payload
        var booking = CreateRandomBooking();
        var json = JsonSerializer.Serialize(booking, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        if (contentType != null)
        {
            // Test case: wrong Content-Type (text/plain) via RestSharp
            var client = ApiClientFactory.Create(Settings.AutomationTestingApiBase);

            var req = new RestRequest("booking", Method.Post)
                .AddHeader("Accept", "application/json")
                .AddHeader("Content-Type", contentType)
                .AddStringBody(json, contentType);

            var resp = await client.ExecuteAsync(req);
            BookingTestHelper.LogRequestResponse(Output, $"POST /booking (Content-Type: {contentType})", resp);

            var status = (int)resp.StatusCode;
            if (status >= 200 && status < 300)
                throw new Xunit.Sdk.XunitException($"Server accepted wrong Content-Type '{contentType}'. Status: {status}. Body: {resp.Content}");

            (status >= 400 && status < 500).Should().BeTrue("Requests with wrong Content-Type should be rejected with 4xx");

            if (!string.IsNullOrWhiteSpace(resp.Content) && resp.Content.TrimStart().StartsWith("{"))
            {
                using var doc = JsonDocument.Parse(resp.Content);
                doc.RootElement.TryGetProperty("bookingid", out _).Should().BeFalse("response for wrong Content-Type must not return bookingid");
            }
        }
        else
        {
            // Test case: missing Content-Type — use HttpClient and remove header
            using var httpClient = new HttpClient();
            var url = $"{Settings.AutomationTestingApiBase.TrimEnd('/')}/booking";
            var httpReq = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8)
            };
            // Remove Content-Type header to simulate missing header
            httpReq.Content.Headers.Remove("Content-Type");

            var httpResp = await httpClient.SendAsync(httpReq);
            var respBody = await httpResp.Content.ReadAsStringAsync();

            Output.WriteLine($"Request: POST {url} (no Content-Type)");
            Output.WriteLine($"Status: {(int)httpResp.StatusCode} - {httpResp.StatusCode}");
            Output.WriteLine($"Body: {respBody ?? "<null>"}");

            var status = (int)httpResp.StatusCode;
            if (status >= 200 && status < 300)
                throw new Xunit.Sdk.XunitException($"Server accepted request without Content-Type. Status: {status}. Body: {respBody}");

            (status >= 400 && status < 500).Should().BeTrue("Requests without Content-Type should be rejected with 4xx");

            if (!string.IsNullOrWhiteSpace(respBody) && respBody.TrimStart().StartsWith("{"))
            {
                using var doc = JsonDocument.Parse(respBody);
                doc.RootElement.TryGetProperty("bookingid", out _).Should().BeFalse("response for missing Content-Type must not return bookingid");
            }
        }
    }

    [Fact(DisplayName = "API-18 - Special characters / injection strings are treated as plain text")]
    public async Task CreateBooking_WithInjectionLikeStrings_TreatedAsPlainText()
    {
        // Preconditions
        Output.Should().NotBeNull();
        // Arrange - payload with potentially dangerous strings
        var script = "<script>alert(1)</script>";
        var sql = "Robert'); DROP TABLE Students;--";
        var booking = CreateRandomBooking();
        booking.firstname = script;
        booking.lastname = sql;
        booking.additionalneeds = "<img src=x onerror=alert(1)>";
        var client = new BookingClient(ApiClientFactory.Create(Settings.AutomationTestingApiBase));

        // Act - create booking
        var createResp = await client.CreateBookingAsync(booking);

        // Log
        BookingTestHelper.LogRequestResponse(Output, "POST /booking (injection strings)", createResp);

        // Assert - server must not crash (no 5xx). Accept 200/201 or controlled 4xx
        var code = (int)createResp.StatusCode;
        if (code >= 500 && code < 600)
            throw new Xunit.Sdk.XunitException($"Server error when submitting injection-like payload. Status: {code}. Body: {createResp.Content}");

        // If accepted, validate response contains bookingid and that stored data preserves plain text
        if (code >= 200 && code < 300)
        {
            createResp.Content.Should().NotBeNullOrWhiteSpace();
            using var createdDoc = JsonDocument.Parse(createResp.Content!);
            createdDoc.RootElement.TryGetProperty("bookingid", out var idEl).Should().BeTrue();
            var bookingId = idEl.GetInt32();
            Output.WriteLine($"Created booking id: {bookingId}");

            // GET the booking and assert fields match raw input (treated as plain text)
            var helper = new BookingApiHelper(ApiClientFactory.Create(Settings.AutomationTestingApiBase));
            var getResp = await helper.GetBookingRawAsync(bookingId);
            BookingTestHelper.LogRequestResponse(Output, $"GET /booking/{bookingId}", getResp);
            getResp.StatusCode.Should().Be(HttpStatusCode.OK);

            using var getDoc = JsonDocument.Parse(getResp.Content!);
            var root = getDoc.RootElement;
            root.GetProperty("firstname").GetString().Should().Be(script);
            root.GetProperty("lastname").GetString().Should().Be(sql);
            if (root.TryGetProperty("additionalneeds", out var addEl) && addEl.ValueKind == JsonValueKind.String)
                addEl.GetString().Should().Be(booking.additionalneeds);
        }
        else if (code >= 400 && code < 500)
        {
            // Controlled validation rejection acceptable; ensure response describes error (optional)
            createResp.Content.Should().NotBeNullOrWhiteSpace();
        }
        else
        {
            throw new Xunit.Sdk.XunitException($"Unexpected status {code} for injection-like payload. Body: {createResp.Content}");
        }
    }

    [Theory(DisplayName = "API-19 - Reject POST /booking with wrong or missing Content-Type")]
    [InlineData("text/plain")]      // use RestSharp with wrong Content-Type
    [InlineData("__no-header__")]   // use HttpClient and remove Content-Type header
    public async Task CreateBooking_WrongOrMissingContentType_IsRejected(string scenario)
    {
        // Preconditions
        Output.Should().NotBeNull();

        // Arrange - normal booking payload
        var booking = CreateRandomBooking();
        var json = JsonSerializer.Serialize(
            booking,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        if (scenario != "__no-header__")
        {
            // Test 1: wrong Content-Type via RestSharp
            var client = ApiClientFactory.Create(Settings.AutomationTestingApiBase);

            var reqText = new RestRequest("booking", Method.Post)
                .AddHeader("Accept", "application/json")
                .AddHeader("Content-Type", scenario)
                .AddStringBody(json, scenario);

            var respText = await client.ExecuteAsync(reqText);
            BookingTestHelper.LogRequestResponse(Output, $"POST /booking (Content-Type: {scenario})", respText);

            var codeText = (int)respText.StatusCode;

            // If server accepted (2xx) — attempt cleanup and fail for triage
            if (codeText >= 200 && codeText < 300)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(respText.Content) && respText.Content.TrimStart().StartsWith("{"))
                    {
                        using var docC = JsonDocument.Parse(respText.Content);
                        if (docC.RootElement.TryGetProperty("bookingid", out var createdIdEl) && createdIdEl.ValueKind == JsonValueKind.Number)
                        {
                            var createdId = createdIdEl.GetInt32();
                            Output.WriteLine($"Server accepted wrong Content-Type and created booking id {createdId}. Attempting cleanup.");

                            try
                            {
                                var bookingClientCleanup = new BookingClient(ApiClientFactory.Create(Settings.AutomationTestingApiBase));
                                var auth = new AutomationTestingAuthClient();
                                var token = await auth.GetTokenAsync("admin", "password");
                                await bookingClientCleanup.DeleteBookingAsync(createdId, token);
                                Output.WriteLine($"Cleanup DELETE /booking/{createdId} attempted.");
                            }
                            catch (Exception ex)
                            {
                                Output.WriteLine($"Cleanup failed: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Output.WriteLine($"Parsing/cleanup error: {ex.Message}");
                }

                throw new Xunit.Sdk.XunitException($"Server accepted wrong Content-Type '{scenario}' (status {codeText}). This is a validation regression. Response: {respText.Content}");
            }

            // Otherwise expect a 4xx rejection
            (codeText >= 400 && codeText < 500).Should().BeTrue("Requests with wrong Content-Type should be rejected with 4xx");

            if (!string.IsNullOrWhiteSpace(respText.Content) && respText.Content.TrimStart().StartsWith("{"))
            {
                using var doc = JsonDocument.Parse(respText.Content);
                doc.RootElement.TryGetProperty("bookingid", out _).Should().BeFalse("response for wrong Content-Type must not return bookingid");
            }
        }
        else
        {
            // Test 2: Missing Content-Type header — use HttpClient to send body with no Content-Type
            using var httpClient = new HttpClient();
            var url = $"{Settings.AutomationTestingApiBase.TrimEnd('/')}/booking";
            var httpReq = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8)
            };
            // remove Content-Type header to simulate missing header
            httpReq.Content.Headers.Remove("Content-Type");

            var httpResp = await httpClient.SendAsync(httpReq);
            var respNoHeaderBody = await httpResp.Content.ReadAsStringAsync();

            // Log similar to BookingTestHelper
            Output.WriteLine($"Request: POST {url} (no Content-Type)");
            Output.WriteLine($"Status: {(int)httpResp.StatusCode} - {httpResp.StatusCode}");
            Output.WriteLine($"Body: {respNoHeaderBody ?? "<null>"}");

            var codeNoHeader = (int)httpResp.StatusCode;

            // If server accepted (2xx) — attempt cleanup and fail for triage
            if (codeNoHeader >= 200 && codeNoHeader < 300)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(respNoHeaderBody) && respNoHeaderBody.TrimStart().StartsWith("{"))
                    {
                        using var docC = JsonDocument.Parse(respNoHeaderBody);
                        if (docC.RootElement.TryGetProperty("bookingid", out var createdIdEl) && createdIdEl.ValueKind == JsonValueKind.Number)
                        {
                            var createdId = createdIdEl.GetInt32();
                            Output.WriteLine($"Server accepted missing Content-Type and created booking id {createdId}. Attempting cleanup.");

                            try
                            {
                                var bookingClientCleanup = new BookingClient(ApiClientFactory.Create(Settings.AutomationTestingApiBase));
                                var auth = new AutomationTestingAuthClient();
                                var token = await auth.GetTokenAsync("admin", "password");
                                await bookingClientCleanup.DeleteBookingAsync(createdId, token);
                                Output.WriteLine($"Cleanup DELETE /booking/{createdId} attempted.");
                            }
                            catch (Exception ex)
                            {
                                Output.WriteLine($"Cleanup failed: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Output.WriteLine($"Parsing/cleanup error: {ex.Message}");
                }

                throw new Xunit.Sdk.XunitException($"Server accepted request without Content-Type (status {codeNoHeader}). This is a validation regression. Response: {respNoHeaderBody}");
            }

            // Otherwise expect a 4xx rejection
            (codeNoHeader >= 400 && codeNoHeader < 500).Should().BeTrue("Requests without Content-Type should be rejected with 4xx");

            if (!string.IsNullOrWhiteSpace(respNoHeaderBody) && respNoHeaderBody.TrimStart().StartsWith("{"))
            {
                using var doc = JsonDocument.Parse(respNoHeaderBody);
                doc.RootElement.TryGetProperty("bookingid", out _).Should().BeFalse("response for missing Content-Type must not return bookingid");
            }
        }
    }

    [Fact(DisplayName = "API-20 - Schema consistency for /booking list and /auth")]
    public async Task ResponseSchemas_BookingListAndAuth_AreConsistent()
    {
        Output.Should().NotBeNull();
        // 0) get token for automation API
        var authReq = new RestRequest("auth/login", Method.Post)
            .AddHeader("Accept", "application/json")
            .AddHeader("Content-Type", "application/json")
            .AddJsonBody(new { username = "admin", password = "password" });
        var authResp = await ApiClientFactory.Create(Settings.AutomationTestingApiBase).ExecuteAsync(authReq);
        BookingTestHelper.LogRequestResponse(Output, "POST /auth/login", authResp);
        authResp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var authDoc = JsonDocument.Parse(authResp.Content!);
        authDoc.RootElement.TryGetProperty("token", out var tokenEl).Should().BeTrue();
        var token = tokenEl.GetString();
        token.Should().NotBeNullOrWhiteSpace();

        // 1) Create booking to ensure at least one exists (use automation API)
        var booking = CreateRandomBooking();
        var bookingClient = new BookingClient(ApiClientFactory.Create(Settings.AutomationTestingApiBase));
        var createResp = await bookingClient.CreateBookingAsync(booking);
        createResp.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        createResp.Content.Should().NotBeNullOrWhiteSpace();
        using var createdDoc = JsonDocument.Parse(createResp.Content!);
        createdDoc.RootElement.TryGetProperty("bookingid", out var idEl).Should().BeTrue();
        var bookingId = idEl.GetInt32();

        // read roomid from created booking
        int roomId = booking.roomid;

        // 2) GET /booking?roomid={roomid} with auth
        var client = ApiClientFactory.Create(Settings.AutomationTestingApiBase);
        var listReq = new RestRequest("booking", Method.Get)
            .AddQueryParameter("roomid", roomId.ToString())
            .AddHeader("Accept", "*/*")
            .AddHeader("Referer", "")
            .AddHeader("Cookie", $"token={token}");
        var listResp = await client.ExecuteAsync(listReq);
        BookingTestHelper.LogRequestResponse(Output, "GET /booking?roomid={roomId}", listResp);
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var listDoc = JsonDocument.Parse(listResp.Content!);
        var root = listDoc.RootElement;
        root.TryGetProperty("bookings", out var bookingsEl).Should().BeTrue();
        bookingsEl.ValueKind.Should().Be(JsonValueKind.Array);

        var first = bookingsEl.EnumerateArray().FirstOrDefault();
        first.ValueKind.Should().Be(JsonValueKind.Object);
        first.TryGetProperty("bookingid", out var bid).Should().BeTrue();
        bid.ValueKind.Should().Be(JsonValueKind.Number);
        first.TryGetProperty("roomid", out var rid).Should().BeTrue();
        rid.ValueKind.Should().Be(JsonValueKind.Number);
        first.TryGetProperty("firstname", out var fn).Should().BeTrue();
        fn.ValueKind.Should().Be(JsonValueKind.String);
        first.TryGetProperty("lastname", out var ln).Should().BeTrue();
        ln.ValueKind.Should().Be(JsonValueKind.String);
        first.TryGetProperty("depositpaid", out var dp).Should().BeTrue();
        (dp.ValueKind == JsonValueKind.True || dp.ValueKind == JsonValueKind.False).Should().BeTrue();
        first.TryGetProperty("bookingdates", out var bd).Should().BeTrue();
        bd.ValueKind.Should().Be(JsonValueKind.Object);
        bd.TryGetProperty("checkin", out var ci).Should().BeTrue();
        ci.ValueKind.Should().Be(JsonValueKind.String);
        bd.TryGetProperty("checkout", out var co).Should().BeTrue();
        co.ValueKind.Should().Be(JsonValueKind.String);

        // 3) GET /booking/{id} with auth — expect same core fields
        var getReq = new RestRequest($"booking/{bookingId}", Method.Get)
            .AddHeader("Accept", "*/*")
            .AddHeader("Referer", "")
            .AddHeader("Cookie", $"token={token}");
        var getResp = await client.ExecuteAsync(getReq);
        BookingTestHelper.LogRequestResponse(Output, $"GET /booking/{bookingId}", getResp);
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var getDoc = JsonDocument.Parse(getResp.Content!);
        var g = getDoc.RootElement;
        g.GetProperty("firstname").ValueKind.Should().Be(JsonValueKind.String);
        g.GetProperty("lastname").ValueKind.Should().Be(JsonValueKind.String);
        var deposit = g.GetProperty("depositpaid").ValueKind;
        (deposit == JsonValueKind.True || deposit == JsonValueKind.False).Should().BeTrue();
        g.GetProperty("bookingdates").ValueKind.Should().Be(JsonValueKind.Object);
        g.GetProperty("bookingdates").GetProperty("checkin").ValueKind.Should().Be(JsonValueKind.String);
        g.GetProperty("bookingdates").GetProperty("checkout").ValueKind.Should().Be(JsonValueKind.String);

    }
}
