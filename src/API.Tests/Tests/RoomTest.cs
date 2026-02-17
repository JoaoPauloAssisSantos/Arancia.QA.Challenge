using Arancia.Test.API.Clients;
using Arancia.Test.API.Helpers;
using FluentAssertions;
using RestSharp;
using System.Net;
using System.Text.Json;
using Xunit.Abstractions;

public class RoomTests : TestBase
{
    public RoomTests(ITestOutputHelper output) => InitTestBase(output);
    [Fact(DisplayName = "API-21 - Create a room with valid data")]
    public async Task CreateRoomWithValidData()
    {
        Output.Should().NotBeNull();

        // Arrange - build room using RoomFactory
        var room = RoomFactory.Create(roomName: "700", type: "Suite", accessible: true,
            image: "https://blog.postman.com/wp-content/uploads/2014/07/logo.png",
            description: "This is room 700, dare you enter?", roomPrice: 100, features: new[] { "WiFi", "Safe" });

        // Auth - obtain token from automation auth
        var auth = new AutomationTestingAuthClient();
        var token = await auth.GetTokenAsync("admin", "password");
        token.Should().NotBeNullOrWhiteSpace();
        Output.WriteLine($"Token: {token}");

        // Use RoomClient for all room operations
        var roomClient = new RoomClient(ApiClientFactory.Create(Settings.AutomationTestingApiBase));

        // Act - POST /api/room
        var createResp = await roomClient.CreateRoomAsync(room, token);
        BookingTestHelper.LogRequestResponse(Output, "POST /api/room", createResp);

        // Assert creation response
        createResp.StatusCode.Should().Be(HttpStatusCode.OK);
        createResp.Content.Should().NotBeNullOrWhiteSpace();
        using var createDoc = JsonDocument.Parse(createResp.Content!);
        createDoc.RootElement.TryGetProperty("success", out var succEl).Should().BeTrue("response should include success");
        succEl.GetBoolean().Should().BeTrue();

        // Find created room id via GET /room (authenticated)
        var listResp = await roomClient.GetRoomsAsync(token);
        BookingTestHelper.LogRequestResponse(Output, "GET /room", listResp);
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var listDoc = JsonDocument.Parse(listResp.Content!);
        listDoc.RootElement.TryGetProperty("rooms", out var roomsEl).Should().BeTrue();
        int createdId = -1;
        foreach (var r in roomsEl.EnumerateArray())
        {
            if (r.TryGetProperty("roomName", out var rn) && rn.GetString() == room.RoomName &&
                r.TryGetProperty("roomid", out var rid) && rid.ValueKind == JsonValueKind.Number)
            {
                createdId = rid.GetInt32();
                break;
            }
        }
        createdId.Should().BeGreaterThan(0, "created room must appear in rooms list");

        // Verify via GET /room/{id}
        var getResp = await roomClient.GetRoomAsync(createdId, token);
        BookingTestHelper.LogRequestResponse(Output, $"GET /room/{createdId}", getResp);
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        getResp.Content.Should().NotBeNullOrWhiteSpace();

        using var getDoc = JsonDocument.Parse(getResp.Content!);
        var root = getDoc.RootElement;
        root.GetProperty("roomid").GetInt32().Should().Be(createdId);
        root.GetProperty("roomName").GetString().Should().Be(room.RoomName);
        root.GetProperty("type").GetString().Should().Be(room.Type);
        root.GetProperty("accessible").GetBoolean().Should().Be(room.Accessible);
        root.GetProperty("roomPrice").GetInt32().Should().Be(room.RoomPrice);

        // Cleanup: delete created room
        var delResp = await roomClient.DeleteRoomAsync(createdId, token);
        BookingTestHelper.LogRequestResponse(Output, $"DELETE /room/{createdId}", delResp);
        ((int)delResp.StatusCode).Should().BeOneOf(200, 201, 204);

    }

    [Theory(DisplayName = "API-22 - Create a room without valid data")]
    [InlineData("emptyRoomName")]
    [InlineData("emptyType")]
    [InlineData("nullAccessible")]
    [InlineData("invalidPriceString")]
    [InlineData("missingFeatures")]
    [InlineData("invalidFeaturesType")]
    public async Task CreateRoom_WithInvalidPayloads_Returns4xxOrServerError(string caseId)
    {
        Output.Should().NotBeNull();
        // build base valid payload via RoomFactory then convert to mutable dictionary
        var baseRoom = RoomFactory.Create();
        var opts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var jsonBase = JsonSerializer.Serialize(baseRoom, opts);
        var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(jsonBase, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!
                        .ToDictionary(k => k.Key, v => v.Value);

        // mutate according to case
        switch (caseId)
        {
            case "emptyRoomName":
                dict["roomName"] = "";
                break;
            case "emptyType":
                dict["type"] = "";
                break;
            case "nullAccessible":
                dict.Remove("accessible"); // omit field (or set to null below)
                break;
            case "invalidPriceString":
                dict["roomPrice"] = "abc"; // invalid type
                break;
            case "missingFeatures":
                dict.Remove("features");
                break;
            case "invalidFeaturesType":
                dict["features"] = "not-an-array";
                break;
            default:
                throw new InvalidOperationException(caseId);
        }

        var payloadJson = JsonSerializer.Serialize(dict, opts);

        // get token
        var auth = new AutomationTestingAuthClient();
        var token = await auth.GetTokenAsync("admin", "password");

        // execute request
        var client = ApiClientFactory.Create(Settings.AutomationTestingApiBase);
        var req = new RestRequest("room", Method.Post)
            .AddHeader("Accept", "*/*")
            .AddHeader("Referer", "")
            .AddHeader("Content-Type", "application/json")
            .AddHeader("Cookie", $"token={token}")
            .AddStringBody(payloadJson, "application/json");

        var resp = await client.ExecuteAsync(req);

        BookingTestHelper.LogRequestResponse(Output, $"POST /room - case {caseId}", resp);

        var code = (int)resp.StatusCode;

        // Acceptable: 4xx (client validation). If server returns 2xx, attempt cleanup and fail.
        if (code >= 400 && code < 500)
        {
            code.Should().BeInRange(400, 499);
            return;
        }

        if (code >= 200 && code < 300)
        {
            // try cleanup if created id returned
            try
            {
                if (!string.IsNullOrWhiteSpace(resp.Content) && resp.Content.TrimStart().StartsWith("{"))
                {
                    using var doc = JsonDocument.Parse(resp.Content);
                    if (doc.RootElement.TryGetProperty("roomId", out var rid) && rid.ValueKind == JsonValueKind.Number)
                    {
                        var createdId = rid.GetInt32();
                        var bookingClientCleanup = new BookingClient(ApiClientFactory.Create(Settings.AutomationTestingApiBase));
                        var delResp = await bookingClientCleanup.DeleteBookingAsync(createdId, token); // best-effort
                        Output.WriteLine($"Cleanup attempted for id {createdId}, status {(int)delResp.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                Output.WriteLine($"Cleanup failed: {ex.Message}");
            }

            throw new Xunit.Sdk.XunitException($"Server accepted invalid room payload for case '{caseId}'. Status: {code}. Response: {resp.Content}");
        }

        if (code >= 500)
            throw new Xunit.Sdk.XunitException($"Server error for invalid room payload (case '{caseId}'). Status: {code}. Response: {resp.Content}");

        throw new Xunit.Sdk.XunitException($"Unexpected status {code} for case '{caseId}'. Response: {resp.Content}");
    }

    [Fact(DisplayName = "API-23 - Create Room And Get Room")]
    public async Task CreateRoomAndGetRoom()
    {
        // Arrange
        var room = RoomFactory.Create();

        var auth = new AutomationTestingAuthClient();
        var token = await auth.GetTokenAsync("admin", "password");
        token.Should().NotBeNullOrWhiteSpace();

        var roomClient = new RoomClient(ApiClientFactory.Create(Settings.AutomationTestingApiBase));

        // Create room
        var createResp = await roomClient.CreateRoomAsync(room, token);
        BookingTestHelper.LogRequestResponse(Output, "POST /api/room", createResp);
        createResp.StatusCode.Should().Be(HttpStatusCode.OK);
        createResp.Content.Should().NotBeNullOrWhiteSpace();
        using var createDoc = JsonDocument.Parse(createResp.Content!);
        createDoc.RootElement.TryGetProperty("success", out var succ).Should().BeTrue();
        succ.GetBoolean().Should().BeTrue();

        // Get rooms list and find created room id
        var listResp = await roomClient.GetRoomsAsync(token);
        BookingTestHelper.LogRequestResponse(Output, "GET /room", listResp);
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var listDoc = JsonDocument.Parse(listResp.Content!);
        listDoc.RootElement.TryGetProperty("rooms", out var roomsEl).Should().BeTrue();
        roomsEl.ValueKind.Should().Be(JsonValueKind.Array);

        int foundId = -1;
        foreach (var r in roomsEl.EnumerateArray())
        {
            if (r.TryGetProperty("roomName", out var rn) && rn.GetString() == room.RoomName
                && r.TryGetProperty("roomid", out var rid) && rid.ValueKind == JsonValueKind.Number)
            {
                foundId = rid.GetInt32();
                break;
            }
        }

        foundId.Should().BeGreaterThan(0, "created room must appear in rooms list");

        // GET /room/{id}
        var getResp = await roomClient.GetRoomAsync(foundId, token);
        BookingTestHelper.LogRequestResponse(Output, $"GET /room/{foundId}", getResp);
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        getResp.Content.Should().NotBeNullOrWhiteSpace();

        using var getDoc = JsonDocument.Parse(getResp.Content!);
        var root = getDoc.RootElement;

        root.GetProperty("roomid").GetInt32().Should().Be(foundId);
        root.GetProperty("roomName").GetString().Should().Be(room.RoomName);
        root.GetProperty("type").GetString().Should().Be(room.Type);
        root.GetProperty("accessible").GetBoolean().Should().Be(room.Accessible);
        root.GetProperty("roomPrice").GetInt32().Should().Be(room.RoomPrice);
        root.TryGetProperty("image", out var img).Should().BeTrue();
        root.TryGetProperty("description", out var desc).Should().BeTrue();
        root.TryGetProperty("features", out var feats).Should().BeTrue();
        feats.ValueKind.Should().Be(JsonValueKind.Array);

        // Cleanup
        var delResp = await roomClient.DeleteRoomAsync(foundId, token);
        BookingTestHelper.LogRequestResponse(Output, $"DELETE /room/{foundId}", delResp);
        ((int)delResp.StatusCode).Should().BeOneOf(200, 201, 204);

    }

    [Fact(DisplayName = "API-24 - Create Room Without valid Auth")]
    public async Task CreateRoomWithoutvalidAuth()
    {
        Output.Should().NotBeNull();
        // Arrange - build room payload
        var room = RoomFactory.Create();
        var opts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(room, opts);

        var client = ApiClientFactory.Create(Settings.AutomationTestingApiBase);

        // Act - POST /api/room WITHOUT auth (no Cookie header)
        var req = new RestRequest("room", Method.Post)
            .AddHeader("Accept", "*/*")
            .AddHeader("Content-Type", "application/json")
            .AddHeader("Referer", "")
            .AddStringBody(json, "application/json");

        var resp = await client.ExecuteAsync(req);

        // Log
        BookingTestHelper.LogRequestResponse(Output, "POST /api/room (no auth)", resp);

        // Assert - expect 401/403 or other 4xx rejection. Fail if 2xx.
        if (resp.StatusCode == HttpStatusCode.Unauthorized)
        {
            resp.Content.Should().NotBeNullOrWhiteSpace();
            using var doc = JsonDocument.Parse(resp.Content!);
            var root = doc.RootElement;

            // Accept either "error": "message" OR "errors": [ "message" ]
            if (root.TryGetProperty("error", out var e))
            {
                e.GetString().Should().MatchRegex("(?i)auth|authentication|required");
            }
            else if (root.TryGetProperty("errors", out var errorsEl) && errorsEl.ValueKind == JsonValueKind.Array)
            {
                var first = errorsEl.EnumerateArray().FirstOrDefault().GetString();
                first.Should().MatchRegex("(?i)auth|authentication|required");
            }
            else
            {
                throw new Xunit.Sdk.XunitException($"Unauthorized response body has unexpected shape: {resp.Content}");
            }

            return;
        }

        if (resp.StatusCode == HttpStatusCode.Forbidden)
        {
            resp.Content.Should().NotBeNullOrWhiteSpace();
            using var doc = JsonDocument.Parse(resp.Content!);
            doc.RootElement.TryGetProperty("error", out var e).Should().BeTrue();
            e.GetString().Should().MatchRegex("(?i)failed|forbid|create");
            return;
        }

        var code = (int)resp.StatusCode;
        if (code >= 400 && code < 500)
        {
            // accept other 4xx client rejections
            code.Should().BeInRange(400, 499);
            return;
        }

        if (code >= 200 && code < 300)
        {
            // Server accepted creation without auth — try to extract id and clean up, then fail
            try
            {
                if (!string.IsNullOrWhiteSpace(resp.Content) && resp.Content.TrimStart().StartsWith("{"))
                {
                    using var doc = JsonDocument.Parse(resp.Content);
                    if (doc.RootElement.TryGetProperty("roomid", out var rid) && rid.ValueKind == JsonValueKind.Number)
                    {
                        var createdId = rid.GetInt32();
                        Output.WriteLine($"Room created without auth (id={createdId}). Attempting cleanup.");

                        try
                        {
                            var auth = new AutomationTestingAuthClient();
                            var token = await auth.GetTokenAsync("admin", "password");
                            var roomCleanupClient = ApiClientFactory.Create(Settings.AutomationTestingApiBase);
                            var delReq = new RestRequest($"room/{createdId}", Method.Delete)
                                .AddHeader("Cookie", $"token={token}");
                            await roomCleanupClient.ExecuteAsync(delReq);
                        }
                        catch (Exception ex) { Output.WriteLine($"Cleanup failed: {ex.Message}"); }
                    }
                }
            }
            catch { /* ignore */ }

            throw new Xunit.Sdk.XunitException($"Server accepted room creation without auth (status {code}). Response: {resp.Content}");
        }

        // Unexpected status (e.g., 5xx) -> fail for triage
        throw new Xunit.Sdk.XunitException($"Unexpected status {(int)resp.StatusCode} for unauthenticated create. Response: {resp.Content}");
    }

    [Fact(DisplayName = "API-25 - Get Room Without valid roomid")]
    public async Task GetRoomWithoutValidRoomId()
    {
        Output.Should().NotBeNull();
        // Arrange - create a room to satisfy precondition
        var room = RoomFactory.Create();
        var auth = new AutomationTestingAuthClient();
        var token = await auth.GetTokenAsync("admin", "password");
        token.Should().NotBeNullOrWhiteSpace();

        var roomClient = new RoomClient(ApiClientFactory.Create(Settings.AutomationTestingApiBase));
        var createResp = await roomClient.CreateRoomAsync(room, token);
        BookingTestHelper.LogRequestResponse(Output, "POST /api/room", createResp);
        createResp.StatusCode.Should().Be(HttpStatusCode.OK);
        createResp.Content.Should().NotBeNullOrWhiteSpace();

        // Choose a very large/non-existing id
        const long invalidId = 9_849_494_984L;
        var client = ApiClientFactory.Create(Settings.AutomationTestingApiBase);

        // Act - GET /room/{invalidId} with auth
        var req = new RestRequest($"room/{invalidId}", Method.Get)
            .AddHeader("Accept", "*/*")
            .AddHeader("Referer", "")
            .AddHeader("Cookie", $"token={token}");

        var resp = await client.ExecuteAsync(req);

        // Log
        BookingTestHelper.LogRequestResponse(Output, $"GET /room/{invalidId}", resp);

        // Assert: prefer 400 BadRequest and expected JSON shape; accept other 4xx as defensive
        if (resp.StatusCode == HttpStatusCode.BadRequest)
        {
            resp.Content.Should().NotBeNullOrWhiteSpace();
            using var doc = JsonDocument.Parse(resp.Content!);
            var root = doc.RootElement;

            root.TryGetProperty("timestamp", out var ts).Should().BeTrue("response must include timestamp");
            ts.ValueKind.Should().Be(JsonValueKind.String);

            root.TryGetProperty("status", out var statusEl).Should().BeTrue();
            statusEl.GetInt32().Should().Be(400);

            root.TryGetProperty("error", out var errorEl).Should().BeTrue();
            errorEl.GetString().Should().Be("Bad Request");

            root.TryGetProperty("path", out var pathEl).Should().BeTrue();
            pathEl.GetString().Should().Contain($"/room/{invalidId}");
            return;
        }

        // Accept 404 or 401/403 as alternative behaviors but surface for triage
        if (resp.StatusCode == HttpStatusCode.NotFound)
        {
            resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
            return;
        }

        if (resp.StatusCode == HttpStatusCode.Unauthorized || resp.StatusCode == HttpStatusCode.Forbidden)
        {
            resp.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
            return;
        }

        // Other 4xx acceptable but unexpected — fail with details
        var code = (int)resp.StatusCode;
        if (code >= 400 && code < 500)
        {
            throw new Xunit.Sdk.XunitException($"Unexpected 4xx ({code}) for GET /room/{invalidId}. Response: {resp.Content}");
        }

        // 5xx -> fail for triage
        throw new Xunit.Sdk.XunitException($"Unexpected status {(int)resp.StatusCode} for GET /room/{invalidId}. Response: {resp.Content}");

    }

    [Fact(DisplayName = "API-26 - Update Room with valid Auth")]
    public async Task UpdateRoomWithValidAuth()
    {
        Output.Should().NotBeNull();
        // Arrange - create room
        var room = RoomFactory.Create();
        var auth = new AutomationTestingAuthClient();
        var token = await auth.GetTokenAsync("admin", "password");
        token.Should().NotBeNullOrWhiteSpace();

        var roomClient = new RoomClient(ApiClientFactory.Create(Settings.AutomationTestingApiBase));
        var createResp = await roomClient.CreateRoomAsync(room, token);
        BookingTestHelper.LogRequestResponse(Output, "POST /api/room", createResp);
        createResp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var createDoc = JsonDocument.Parse(createResp.Content!);
        createDoc.RootElement.TryGetProperty("success", out var succ).Should().BeTrue();
        succ.GetBoolean().Should().BeTrue();

        // Find created room id via GetRoomsAsync
        var listResp = await roomClient.GetRoomsAsync(token);
        BookingTestHelper.LogRequestResponse(Output, "GET /room", listResp);
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var listDoc = JsonDocument.Parse(listResp.Content!);
        listDoc.RootElement.TryGetProperty("rooms", out var roomsEl).Should().BeTrue();
        int roomId = -1;
        foreach (var r in roomsEl.EnumerateArray())
        {
            if (r.TryGetProperty("roomName", out var rn) && rn.GetString() == room.RoomName &&
                r.TryGetProperty("roomid", out var rid) && rid.ValueKind == JsonValueKind.Number)
            {
                roomId = rid.GetInt32();
                break;
            }
        }
        roomId.Should().BeGreaterThan(0);

        // Prepare update payload
        var updatedPayload = new
        {
            roomName = room.RoomName,
            type = "Suite",
            accessible = !room.Accessible,
            image = room.Image,
            description = "Updated by tests",
            roomPrice = room.RoomPrice + 10,
            features = room.Features
        };

        // Act - PUT /room/{id} using RoomClient.PutRoomAsync
        var putResp = await roomClient.PutRoomAsync(roomId, updatedPayload, token);
        BookingTestHelper.LogRequestResponse(Output, $"PUT /room/{roomId}", putResp);

        // Assert PUT success
        putResp.StatusCode.Should().Be(HttpStatusCode.OK);
        putResp.Content.Should().NotBeNullOrWhiteSpace();
        using var putDoc = JsonDocument.Parse(putResp.Content!);
        putDoc.RootElement.TryGetProperty("success", out var putSucc).Should().BeTrue();
        putSucc.GetBoolean().Should().BeTrue();

        // Verify via GetRoomAsync
        var getResp = await roomClient.GetRoomAsync(roomId, token);
        BookingTestHelper.LogRequestResponse(Output, $"GET /room/{roomId}", getResp);
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        getResp.Content.Should().NotBeNullOrWhiteSpace();

        using var getDoc = JsonDocument.Parse(getResp.Content!);
        var root = getDoc.RootElement;
        root.GetProperty("roomid").GetInt32().Should().Be(roomId);
        root.GetProperty("type").GetString().Should().Be("Suite");
        root.GetProperty("accessible").GetBoolean().Should().Be(!room.Accessible);
        root.GetProperty("description").GetString().Should().Be("Updated by tests");
        root.GetProperty("roomPrice").GetInt32().Should().Be(room.RoomPrice + 10);

        // Cleanup
        var delResp = await roomClient.DeleteRoomAsync(roomId, token);
        BookingTestHelper.LogRequestResponse(Output, $"DELETE /room/{roomId}", delResp);
        ((int)delResp.StatusCode).Should().BeOneOf(200, 201, 204);
    }

    [Fact(DisplayName = "API-27 - Update Room without valid Auth")]
    public async Task UpdateRoomWithoutValidAuth()
    {
        Output.Should().NotBeNull();
        // Arrange — create room with auth
        var room = RoomFactory.Create();
        var auth = new AutomationTestingAuthClient();
        var token = await auth.GetTokenAsync("admin", "password");
        token.Should().NotBeNullOrWhiteSpace();

        var roomClient = new RoomClient(ApiClientFactory.Create(Settings.AutomationTestingApiBase));
        var createResp = await roomClient.CreateRoomAsync(room, token);
        BookingTestHelper.LogRequestResponse(Output, "POST /api/room", createResp);
        createResp.StatusCode.Should().Be(HttpStatusCode.OK);
        createResp.Content.Should().NotBeNullOrWhiteSpace();

        // discover created room id via RoomClient.GetRoomsAsync
        var listResp = await roomClient.GetRoomsAsync(token);
        BookingTestHelper.LogRequestResponse(Output, "GET /room", listResp);
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var listDoc = JsonDocument.Parse(listResp.Content!);
        listDoc.RootElement.TryGetProperty("rooms", out var roomsEl).Should().BeTrue();
        var created = roomsEl.EnumerateArray()
            .FirstOrDefault(r => r.TryGetProperty("roomName", out var rn) && rn.GetString() == room.RoomName);
        created.ValueKind.Should().Be(JsonValueKind.Object);
        var roomId = created.GetProperty("roomid").GetInt32();
        Output.WriteLine($"Created room id: {roomId}");

        // Prepare update payload
        var updated = new
        {
            roomName = room.RoomName,
            type = "Suite",
            accessible = !room.Accessible,
            image = room.Image,
            description = "Attempt update without auth",
            roomPrice = room.RoomPrice + 10,
            features = room.Features
        };

        // Act — PUT /room/{id} WITHOUT auth (call raw client so no Cookie header is sent)
        var client = ApiClientFactory.Create(Settings.AutomationTestingApiBase);
        var putReq = new RestRequest($"room/{roomId}", Method.Put)
            .AddHeader("Content-Type", "application/json")
            .AddStringBody(JsonSerializer.Serialize(updated, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }), "application/json"); // no Cookie
        var putResp = await client.ExecuteAsync(putReq);
        BookingTestHelper.LogRequestResponse(Output, $"PUT /room/{roomId} (no auth)", putResp);

        // Assert: accept 401/403 OR 500 with {"errors":[...]} per spec; fail on 200
        if (putResp.StatusCode == HttpStatusCode.OK)
        {
            // cleanup then fail (best-effort)
            try
            {
                var adminToken = await auth.GetTokenAsync("admin", "password");
                await roomClient.DeleteRoomAsync(roomId, adminToken);
            }
            catch { /* ignore */ }

            throw new Xunit.Sdk.XunitException($"PUT without auth unexpectedly succeeded (200). Response: {putResp.Content}");
        }

        if (putResp.StatusCode == HttpStatusCode.Unauthorized || putResp.StatusCode == HttpStatusCode.Forbidden)
        {
            if (!string.IsNullOrWhiteSpace(putResp.Content) && putResp.Content.TrimStart().StartsWith("{"))
            {
                using var doc = JsonDocument.Parse(putResp.Content!);
                var root = doc.RootElement;
                if (root.TryGetProperty("error", out var e)) e.GetString().Should().MatchRegex("(?i)auth|authentication|required");
                else if (root.TryGetProperty("errors", out var errs) && errs.ValueKind == JsonValueKind.Array)
                {
                    var first = errs.EnumerateArray().Select(x => x.GetString()).FirstOrDefault();
                    first.Should().NotBeNullOrWhiteSpace();
                }
            }
            return;
        }

        if (putResp.StatusCode == HttpStatusCode.InternalServerError)
        {
            putResp.Content.Should().NotBeNullOrWhiteSpace();
            using var doc = JsonDocument.Parse(putResp.Content!);
            doc.RootElement.TryGetProperty("errors", out var errorsEl).Should().BeTrue("expected 'errors' array on 500");
            errorsEl.ValueKind.Should().Be(JsonValueKind.Array);
            var msg = errorsEl.EnumerateArray().Select(e => e.GetString()).FirstOrDefault();
            msg.Should().Contain("unexpected", "expected server error message about unexpected error");
            return;
        }

        var code = (int)putResp.StatusCode;
        if (code >= 400 && code < 500)
        {
            code.Should().BeInRange(400, 499);
            return;
        }

        throw new Xunit.Sdk.XunitException($"Unexpected status {(int)putResp.StatusCode} for PUT without auth. Response: {putResp.Content}");
    }

    [Theory(DisplayName = "API-28 - Update Room without valid Data")]
    [InlineData("emptyRoomName")]
    [InlineData("emptyType")]
    [InlineData("accessibleAsNumber")]
    [InlineData("hugePrice")]
    [InlineData("missingFeatures")]
    public async Task UpdateRoom_WithInvalidData_ReturnsClientError(string caseId)
    {
        Output.Should().NotBeNull();
        // Arrange — create a valid room and obtain auth token
        var room = RoomFactory.Create();
        var auth = new AutomationTestingAuthClient();
        var token = await auth.GetTokenAsync("admin", "password");
        token.Should().NotBeNullOrWhiteSpace();

        var roomClient = new RoomClient(ApiClientFactory.Create(Settings.AutomationTestingApiBase));
        var createResp = await roomClient.CreateRoomAsync(room, token);
        BookingTestHelper.LogRequestResponse(Output, "POST /api/room", createResp);
        createResp.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);

        // discover created room id via RoomClient.GetRoomsAsync
        var listResp = await roomClient.GetRoomsAsync(token);
        BookingTestHelper.LogRequestResponse(Output, "GET /room", listResp);
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var listDoc = JsonDocument.Parse(listResp.Content!);
        listDoc.RootElement.TryGetProperty("rooms", out var roomsEl).Should().BeTrue();
        int roomId = -1;
        foreach (var r in roomsEl.EnumerateArray())
        {
            if (r.TryGetProperty("roomName", out var rn) && rn.GetString() == room.RoomName
                && r.TryGetProperty("roomid", out var rid) && rid.ValueKind == JsonValueKind.Number)
            {
                roomId = rid.GetInt32();
                break;
            }
        }
        roomId.Should().BeGreaterThan(0);

        // build invalid payload based on case
        object updatedPayload = caseId switch
        {
            "emptyRoomName" => new
            {
                roomName = "",
                type = room.Type,
                accessible = room.Accessible,
                image = room.Image,
                description = room.Description,
                roomPrice = room.RoomPrice,
                features = room.Features
            },
            "emptyType" => new
            {
                roomName = room.RoomName,
                type = "",
                accessible = room.Accessible,
                image = room.Image,
                description = room.Description,
                roomPrice = room.RoomPrice,
                features = room.Features
            },
            "accessibleAsNumber" => new
            {
                roomName = room.RoomName,
                type = room.Type,
                accessible = 0, // invalid type
                image = room.Image,
                description = room.Description,
                roomPrice = room.RoomPrice,
                features = room.Features
            },
            "hugePrice" => new
            {
                roomName = room.RoomName,
                type = room.Type,
                accessible = room.Accessible,
                image = room.Image,
                description = room.Description,
                roomPrice = 8_000_000,
                features = room.Features
            },
            "missingFeatures" => new
            {
                roomName = room.RoomName,
                type = room.Type,
                accessible = room.Accessible,
                image = room.Image,
                description = room.Description,
                roomPrice = room.RoomPrice,
                features = (string[]?)null
            },
            _ => throw new InvalidOperationException(caseId)
        };

        // Act - PUT /room/{id} with auth via RoomClient.PutRoomAsync
        var putResp = await roomClient.PutRoomAsync(roomId, updatedPayload, token);
        BookingTestHelper.LogRequestResponse(Output, $"PUT /room/{roomId} - case {caseId}", putResp);

        // Assert - expect 4xx client error; 500 => fail for triage; if 2xx then cleanup and fail
        var code = (int)putResp.StatusCode;
        if (code >= 400 && code < 500)
        {
            code.Should().BeInRange(400, 499);
            if (!string.IsNullOrWhiteSpace(putResp.Content) && putResp.Content.TrimStart().StartsWith("{"))
            {
                using var doc = JsonDocument.Parse(putResp.Content!);
                if (doc.RootElement.TryGetProperty("errors", out var errorsEl) && errorsEl.ValueKind == JsonValueKind.Array)
                    errorsEl.EnumerateArray().Should().NotBeEmpty();
            }
            return;
        }

        if (code >= 200 && code < 300)
        {
            // cleanup created room then fail
            try
            {
                await roomClient.DeleteRoomAsync(roomId, token);
            }
            catch { /* best-effort */ }

            throw new Xunit.Sdk.XunitException($"Server accepted invalid update for case '{caseId}'. Status: {code}. Response: {putResp.Content}");
        }

        if (code >= 500)
            throw new Xunit.Sdk.XunitException($"Server error for invalid update (case '{caseId}'). Status: {code}. Response: {putResp.Content}");

        throw new Xunit.Sdk.XunitException($"Unexpected status {code} for case '{caseId}'. Response: {putResp.Content}");

    }


    [Fact(DisplayName = "API-29 - Delete Room with valid Auth")]
    public async Task DeleteRoomWithValidAuth_RemovesRoom()
    {
        Output.Should().NotBeNull();

        string? token = null;
        var auth = new AutomationTestingAuthClient();

        try
        {
            // Arrange - create room
            var room = RoomFactory.Create();
            token = await auth.GetTokenAsync("admin", "password");
            token.Should().NotBeNullOrWhiteSpace();

            var roomClient = new RoomClient(ApiClientFactory.Create(Settings.AutomationTestingApiBase));
            var createResp = await roomClient.CreateRoomAsync(room, token);
            BookingTestHelper.LogRequestResponse(Output, "POST /api/room", createResp);
            createResp.StatusCode.Should().Be(HttpStatusCode.OK);
            createResp.Content.Should().NotBeNullOrWhiteSpace();

            // Find created room id via GetRoomsAsync
            var listResp = await roomClient.GetRoomsAsync(token);
            BookingTestHelper.LogRequestResponse(Output, "GET /room", listResp);
            listResp.StatusCode.Should().Be(HttpStatusCode.OK);
            using var listDoc = JsonDocument.Parse(listResp.Content!);
            listDoc.RootElement.TryGetProperty("rooms", out var roomsEl).Should().BeTrue();
            int roomId = -1;
            foreach (var r in roomsEl.EnumerateArray())
            {
                if (r.TryGetProperty("roomName", out var rn) && rn.GetString() == room.RoomName
                    && r.TryGetProperty("roomid", out var rid) && rid.ValueKind == JsonValueKind.Number)
                {
                    roomId = rid.GetInt32();
                    break;
                }
            }
            roomId.Should().BeGreaterThan(0);

            // Act - DELETE /room/{id} with auth
            var deleteResp = await roomClient.DeleteRoomAsync(roomId, token);
            BookingTestHelper.LogRequestResponse(Output, $"DELETE /room/{roomId}", deleteResp);

            // Assert delete success (accept 200/201/204)
            ((int)deleteResp.StatusCode).Should().BeOneOf(200, 201, 204);

            // Verify removal: prefer GET /room/{id}
            var getResp = await roomClient.GetRoomAsync(roomId, token);
            BookingTestHelper.LogRequestResponse(Output, $"GET /room/{roomId} after delete", getResp);

            if (getResp.StatusCode == HttpStatusCode.NotFound)
            {
                // expected
                Output.WriteLine($"GET /room/{roomId} returned 404 as expected after delete.");
            }
            else if (getResp.StatusCode == HttpStatusCode.Unauthorized || getResp.StatusCode == HttpStatusCode.Forbidden)
            {
                // protected endpoint, acceptable outcome
                Output.WriteLine($"GET /room/{roomId} returned {(int)getResp.StatusCode} (protected). Accepting as valid post-delete behavior.");
            }
            else if (getResp.StatusCode == HttpStatusCode.InternalServerError)
            {
                // Backend threw 500 — attempt fallback to GET list to confirm removal
                Output.WriteLine($"GET /room/{roomId} returned 500. Attempting fallback GET /room to confirm removal.");

                var listAfterDelete = await roomClient.GetRoomsAsync(token);
                BookingTestHelper.LogRequestResponse(Output, "GET /room (after delete fallback)", listAfterDelete);
                listAfterDelete.StatusCode.Should().Be(HttpStatusCode.OK, "Fallback GET /room should succeed to confirm removal when GET by id returns 500");

                using var listAfterDoc = JsonDocument.Parse(listAfterDelete.Content!);
                listAfterDoc.RootElement.TryGetProperty("rooms", out var roomsAfterEl).Should().BeTrue();
                var existsInList = roomsAfterEl.EnumerateArray()
                    .Any(r => r.TryGetProperty("roomid", out var rid) && rid.ValueKind == JsonValueKind.Number && rid.GetInt32() == roomId);

                existsInList.Should().BeFalse($"Room {roomId} must not be present in GET /room even if GET by id returned 500. Note: backend returned 500 for GET /room/{roomId} and should be triaged.");
            }
            else
            {
                // unexpected status — fail with details
                throw new Xunit.Sdk.XunitException($"Unexpected status {(int)getResp.StatusCode} after delete. Response: {getResp.Content}");
            }
        }
        finally
        {
            // Token teardown – best-effort; do not mask the original test result
            if (!string.IsNullOrWhiteSpace(token))
            {
                try
                {
                    await auth.DestroyTokenAsync(token!);
                    Output.WriteLine("Admin token destroyed successfully after room delete test.");
                }
                catch (Exception ex)
                {
                    Output.WriteLine($"Failed to destroy admin token after room delete test: {ex.Message}");
                }
            }
        }
    }

    [Fact(DisplayName = "API-30 - Delete Room without valid Auth")]
    public async Task DeleteRoomWithValidAuthout_RemovesRoom()
    {
        Output.Should().NotBeNull();
        // Arrange: create room with auth
        var room = RoomFactory.Create();
        var auth = new AutomationTestingAuthClient();
        var token = await auth.GetTokenAsync("admin", "password");
        token.Should().NotBeNullOrWhiteSpace();

        var roomClient = new RoomClient(ApiClientFactory.Create(Settings.AutomationTestingApiBase));
        var createResp = await roomClient.CreateRoomAsync(room, token);
        BookingTestHelper.LogRequestResponse(Output, "POST /api/room", createResp);
        createResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Find created room id
        var listResp = await roomClient.GetRoomsAsync(token);
        BookingTestHelper.LogRequestResponse(Output, "GET /room", listResp);
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var listDoc = JsonDocument.Parse(listResp.Content!);
        listDoc.RootElement.TryGetProperty("rooms", out var roomsEl).Should().BeTrue();
        var created = roomsEl.EnumerateArray()
            .FirstOrDefault(r => r.TryGetProperty("roomName", out var rn) && rn.GetString() == room.RoomName);
        created.ValueKind.Should().Be(JsonValueKind.Object);
        var roomId = created.GetProperty("roomid").GetInt32();
        Output.WriteLine($"Created room id: {roomId}");

        // Act: attempt DELETE without auth (no token)
        var client = ApiClientFactory.Create(Settings.AutomationTestingApiBase);
        var deleteReq = new RestRequest($"room/{roomId}", Method.Delete)
            .AddHeader("Accept", "*/*");
        var deleteResp = await client.ExecuteAsync(deleteReq);
        BookingTestHelper.LogRequestResponse(Output, $"DELETE /room/{roomId} (no auth)", deleteResp);

        // Assert: expect 401/403 or other 4xx; fail if 2xx
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

        // Verify room still exists (GET may require auth)
        var getResp = await roomClient.GetRoomAsync(roomId);
        BookingTestHelper.LogRequestResponse(Output, $"GET /room/{roomId} after failed delete", getResp);

        if (getResp.StatusCode == HttpStatusCode.OK)
        {
            using var getDoc = JsonDocument.Parse(getResp.Content!);
            getDoc.RootElement.GetProperty("roomName").GetString().Should().Be(room.RoomName);
        }
        else if (getResp.StatusCode == HttpStatusCode.Unauthorized || getResp.StatusCode == HttpStatusCode.Forbidden)
        {
            Output.WriteLine($"GET /room/{roomId} requires auth (status {(int)getResp.StatusCode}); cannot verify content but delete was rejected.");
        }
        else
        {
            throw new Xunit.Sdk.XunitException($"Unexpected status {(int)getResp.StatusCode} when verifying room after failed delete. Response: {getResp.Content}");
        }

        // Cleanup: remove room using auth
        try
        {
            var delCleanup = await roomClient.DeleteRoomAsync(roomId, token);
            BookingTestHelper.LogRequestResponse(Output, $"DELETE /room/{roomId} (cleanup)", delCleanup);
        }
        catch (Exception ex)
        {
            Output.WriteLine($"Cleanup failed: {ex.Message}");
        }

    }

    [Fact(DisplayName = "API-31 - Create room and book")]
    public async Task CreateRoomAndBook_BookingCreated()
    {
        Output.Should().NotBeNull();
        // Arrange — create room with auth
        var room = RoomFactory.Create();
        var auth = new AutomationTestingAuthClient();
        var token = await auth.GetTokenAsync("admin", "password");
        token.Should().NotBeNullOrWhiteSpace();

        var roomClient = new RoomClient(ApiClientFactory.Create(Settings.AutomationTestingApiBase));
        var createRoomResp = await roomClient.CreateRoomAsync(room, token);
        BookingTestHelper.LogRequestResponse(Output, "POST /api/room", createRoomResp);
        createRoomResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Find created room id
        var listResp = await roomClient.GetRoomsAsync(token);
        BookingTestHelper.LogRequestResponse(Output, "GET /room", listResp);
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var listDoc = JsonDocument.Parse(listResp.Content!);
        listDoc.RootElement.TryGetProperty("rooms", out var roomsEl).Should().BeTrue();
        var created = roomsEl.EnumerateArray()
            .FirstOrDefault(r => r.TryGetProperty("roomName", out var rn) && rn.GetString() == room.RoomName);
        created.ValueKind.Should().Be(JsonValueKind.Object);
        var roomId = created.GetProperty("roomid").GetInt32();
        Output.WriteLine($"Created room id: {roomId}");

        // Arrange booking for that room
        var booking = CreateRandomBooking();
        booking.roomid = roomId;

        var bookingClient = new BookingClient(ApiClientFactory.Create(Settings.AutomationTestingApiBase));
        var createBookingResp = await bookingClient.CreateBookingAsync(booking);
        BookingTestHelper.LogRequestResponse(Output, "POST /booking", createBookingResp);

        // Assert booking created
        createBookingResp.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        createBookingResp.Content.Should().NotBeNullOrWhiteSpace();
        using var bookingDoc = JsonDocument.Parse(createBookingResp.Content!);
        bookingDoc.RootElement.TryGetProperty("bookingid", out var bookingIdEl).Should().BeTrue();
        var bookingId = bookingIdEl.GetInt32();
        Output.WriteLine($"Created booking id: {bookingId}");

        // Cleanup: delete booking and room (best-effort)
        try
        {
            await bookingClient.DeleteBookingAsync(bookingId, token);
        }
        catch (Exception ex) { Output.WriteLine($"Booking cleanup failed: {ex.Message}"); }

        try
        {
            await roomClient.DeleteRoomAsync(roomId, token);
        }
        catch (Exception ex) { Output.WriteLine($"Room cleanup failed: {ex.Message}"); 
        }
    }
    [Fact(DisplayName = "API-32 - Create room Update room and book")]
    public async Task CreateRoomUpdateRoomAndBook_BookingCreated()
    {
        Output.Should().NotBeNull();
        // Auth + clients
        var auth = new AutomationTestingAuthClient();
        var token = await auth.GetTokenAsync("admin", "password");
        token.Should().NotBeNullOrWhiteSpace();

        var roomClient = new RoomClient(ApiClientFactory.Create(Settings.AutomationTestingApiBase));
        var bookingClient = new BookingClient(ApiClientFactory.Create(Settings.AutomationTestingApiBase));

        // 1) Create room
        var room = RoomFactory.Create();
        var createRoomResp = await roomClient.CreateRoomAsync(room, token);
        BookingTestHelper.LogRequestResponse(Output, "POST /api/room", createRoomResp);
        createRoomResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2) Find created room id
        var listResp = await roomClient.GetRoomsAsync(token);
        BookingTestHelper.LogRequestResponse(Output, "GET /room", listResp);
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var listDoc = JsonDocument.Parse(listResp.Content!);
        listDoc.RootElement.TryGetProperty("rooms", out var roomsEl).Should().BeTrue();
        var created = roomsEl.EnumerateArray()
            .FirstOrDefault(r => r.TryGetProperty("roomName", out var rn) && rn.GetString() == room.RoomName);
        created.ValueKind.Should().Be(JsonValueKind.Object);
        var roomId = created.GetProperty("roomid").GetInt32();
        Output.WriteLine($"Created room id: {roomId}");

        try
        {
            // 3) Update room
            var updatedPayload = new
            {
                roomName = room.RoomName,
                type = "Suite",
                accessible = !room.Accessible,
                image = room.Image,
                description = "Updated before booking",
                roomPrice = room.RoomPrice + 20,
                features = room.Features
            };

            var putResp = await roomClient.PutRoomAsync(roomId, updatedPayload, token);
            BookingTestHelper.LogRequestResponse(Output, $"PUT /room/{roomId}", putResp);
            putResp.StatusCode.Should().Be(HttpStatusCode.OK);
            using var putDoc = JsonDocument.Parse(putResp.Content!);
            putDoc.RootElement.TryGetProperty("success", out var putSucc).Should().BeTrue();
            putSucc.GetBoolean().Should().BeTrue();

            // 4) Create booking for updated room
            var booking = CreateRandomBooking();
            booking.roomid = roomId;
            var createBookingResp = await bookingClient.CreateBookingAsync(booking);
            BookingTestHelper.LogRequestResponse(Output, "POST /booking", createBookingResp);
            createBookingResp.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
            createBookingResp.Content.Should().NotBeNullOrWhiteSpace();
            using var bookingDoc = JsonDocument.Parse(createBookingResp.Content!);
            bookingDoc.RootElement.TryGetProperty("bookingid", out var bookingIdEl).Should().BeTrue();
            var bookingId = bookingIdEl.GetInt32();
            Output.WriteLine($"Created booking id: {bookingId}");

            // Optional: verify booking references roomId by GET /booking/{id}
            var getBookingResp = await bookingClient.GetBookingAsync(bookingId, token);
            BookingTestHelper.LogRequestResponse(Output, $"GET /booking/{bookingId}", getBookingResp);
            getBookingResp.StatusCode.Should().Be(HttpStatusCode.OK);
            using var getBookingDoc = JsonDocument.Parse(getBookingResp.Content!);
            // If API returns roomid in booking, check it (defensive)
            if (getBookingDoc.RootElement.TryGetProperty("roomid", out var bRoomIdEl) && bRoomIdEl.ValueKind == JsonValueKind.Number)
                bRoomIdEl.GetInt32().Should().Be(roomId);
        }
        finally
        {
            // Cleanup: delete booking(s) and room (best-effort)
            try
            {
                // delete possible booking created (if any) - attempt to find latest booking for room
                var bookingsResp = await bookingClient.GetBookingAsync(0); // placeholder to avoid compile error if not implemented
            }
            catch { /* ignore - cleanup best-effort via known ids if stored above in variables */ }

            try { await roomClient.DeleteRoomAsync(roomId, token); } catch (Exception ex) { Output.WriteLine($"Room cleanup failed: {ex.Message}"); }
        }

    }
    [Fact(DisplayName = "API-33 - Creating booking for room already booked on same dates returns 409")]
    public async Task CreateBooking_ForAlreadyBookedRoom_ReturnsConflict()
    {
        Output.Should().NotBeNull();
        // Arrange: create room
        var room = RoomFactory.Create();
        var auth = new AutomationTestingAuthClient();
        var adminToken = await auth.GetTokenAsync("admin", "password");
        adminToken.Should().NotBeNullOrWhiteSpace();

        var roomClient = new RoomClient(ApiClientFactory.Create(Settings.AutomationTestingApiBase));
        var createRoomResp = await roomClient.CreateRoomAsync(room, adminToken);
        BookingTestHelper.LogRequestResponse(Output, "POST /api/room", createRoomResp);
        createRoomResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // find created room id
        var listResp = await roomClient.GetRoomsAsync(adminToken);
        BookingTestHelper.LogRequestResponse(Output, "GET /room", listResp);
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var listDoc = JsonDocument.Parse(listResp.Content!);
        listDoc.RootElement.TryGetProperty("rooms", out var roomsEl).Should().BeTrue();
        var created = roomsEl.EnumerateArray()
            .FirstOrDefault(r => r.TryGetProperty("roomName", out var rn) && rn.GetString() == room.RoomName);
        created.ValueKind.Should().Be(JsonValueKind.Object);
        var roomId = created.GetProperty("roomid").GetInt32();
        Output.WriteLine($"Created room id: {roomId}");

        // Prepare booking dates
        var checkin = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd");
        var checkout = DateTime.UtcNow.AddDays(9).ToString("yyyy-MM-dd");

        // Create first booking for these dates
        var bookingClient = new BookingClient(ApiClientFactory.Create(Settings.AutomationTestingApiBase));
        var booking1 = CreateRandomBooking();
        booking1.roomid = roomId;
        booking1.bookingdates = new BookingDates { checkin = checkin, checkout = checkout };

        var resp1 = await bookingClient.CreateBookingAsync(booking1);
        BookingTestHelper.LogRequestResponse(Output, "POST /booking (first)", resp1);
        resp1.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        resp1.Content.Should().NotBeNullOrWhiteSpace();
        using var doc1 = JsonDocument.Parse(resp1.Content!);
        doc1.RootElement.TryGetProperty("bookingid", out var bookingIdEl1).Should().BeTrue();
        var bookingId1 = bookingIdEl1.GetInt32();
        Output.WriteLine($"Created booking id (first): {bookingId1}");

        // Act: attempt to create second booking for same room and same dates
        var booking2 = CreateRandomBooking();
        booking2.roomid = roomId;
        booking2.bookingdates = new BookingDates { checkin = checkin, checkout = checkout };

        var resp2 = await bookingClient.CreateBookingAsync(booking2);
        BookingTestHelper.LogRequestResponse(Output, "POST /booking (conflict attempt)", resp2);

        // Assert: expect 409 Conflict and error body { "error": "Failed to create booking" }
        resp2.StatusCode.Should().Be(HttpStatusCode.Conflict);
        resp2.Content.Should().NotBeNullOrWhiteSpace();
        using var doc2 = JsonDocument.Parse(resp2.Content!);
        doc2.RootElement.TryGetProperty("error", out var errEl).Should().BeTrue();
        errEl.GetString().Should().Be("Failed to create booking");

        // Cleanup (best-effort): delete created booking and room
        try { await bookingClient.DeleteBookingAsync(bookingId1, adminToken); } catch (Exception ex) { Output.WriteLine($"Booking cleanup failed: {ex.Message}"); }
        try { await roomClient.DeleteRoomAsync(roomId, adminToken); } catch (Exception ex) { Output.WriteLine($"Room cleanup failed: {ex.Message}"); }

    }
    [Fact(DisplayName = "API-34 - Create room and verify appears in GET /room")]
    public async Task CreateRoom_And_VerifyInGetRooms()
    {
        Output.Should().NotBeNull();

        // Auth + clients (reuse same approach as API-31)
        var auth = new AutomationTestingAuthClient();
        var token = await auth.GetTokenAsync("admin", "password");
        token.Should().NotBeNullOrWhiteSpace();

        var roomClient = new RoomClient(ApiClientFactory.Create(Settings.AutomationTestingApiBase));

        // 1) Create unique room payload using factory (or inline)
        var room = RoomFactory.Create();
        // ensure uniqueness to avoid collisions
        room.RoomName = $"{room.RoomName}-{DateTime.UtcNow:yyyyMMddHHmmssfff}";

        var createRoomResp = await roomClient.CreateRoomAsync(room, token);
        BookingTestHelper.LogRequestResponse(Output, "POST /api/room (via RoomClient)", createRoomResp);
        createRoomResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2) GET list of rooms and find the created one (use RoomClient.GetRoomsAsync)
        var listResp = await roomClient.GetRoomsAsync(token);
        BookingTestHelper.LogRequestResponse(Output, "GET /room (via RoomClient)", listResp);
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var listDoc = JsonDocument.Parse(listResp.Content!);
        listDoc.RootElement.TryGetProperty("rooms", out var roomsEl).Should().BeTrue();
        roomsEl.ValueKind.Should().Be(JsonValueKind.Array);

        var created = roomsEl.EnumerateArray()
            .FirstOrDefault(r => r.TryGetProperty("roomName", out var rn) && rn.GetString() == room.RoomName);

        // Assert found and capture id for cleanup
        created.ValueKind.Should().Be(JsonValueKind.Object, $"Created room '{room.RoomName}' must be present in GET /room");
        var roomId = created.GetProperty("roomid").GetInt32();
        Output.WriteLine($"Created room id: {roomId}");

        // Cleanup - best-effort delete created room
        try
        {
            await roomClient.DeleteRoomAsync(roomId, token);
        }
        catch (Exception ex)
        {
            Output.WriteLine($"Room cleanup failed: {ex.Message}");
        }
    }
}