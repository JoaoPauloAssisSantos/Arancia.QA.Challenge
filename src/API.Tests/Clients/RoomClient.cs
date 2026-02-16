
using Arancia.Test.API.Helpers;
using RestSharp;
using System.Text.Json;

namespace Arancia.Test.API.Clients
{
    public class RoomClient
    {
        private readonly RestClient _client;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        public RoomClient(RestClient? client = null) =>
    _client = client ?? ApiClientFactory.Create(Settings.AutomationTestingApiBase);

        public async Task<RestResponse> CreateRoomAsync(Room room, string token)
        {
            var json = JsonSerializer.Serialize(room, _jsonOptions);
            var req = new RestRequest("room", Method.Post)
                .AddHeader("Accept", "*/*")
                .AddHeader("Content-Type", "application/json")
                .AddHeader("Referer", "")
                .AddHeader("Cookie", $"token={token}")
                .AddStringBody(json, "application/json");

            return await _client.ExecuteAsync(req);
        }

        // GET /room/{id}
        public async Task<RestResponse> GetRoomAsync(int roomId, string? token = null)
        {
            var req = new RestRequest($"room/{roomId}", Method.Get)
                .AddHeader("Accept", "*/*")
                .AddHeader("Referer", "");

            if (!string.IsNullOrWhiteSpace(token))
                req.AddHeader("Cookie", $"token={token}");

            return await _client.ExecuteAsync(req);
        }

        // GET /room (list) with optional auth token
        public async Task<RestResponse> GetRoomsAsync(string? token = null)
        {
            var req = new RestRequest("room", Method.Get)
                .AddHeader("Accept", "*/*")
                .AddHeader("Referer", "");

            if (!string.IsNullOrWhiteSpace(token))
                req.AddHeader("Cookie", $"token={token}");

            return await _client.ExecuteAsync(req);
        }

        // DELETE /room/{id}
        public async Task<RestResponse> DeleteRoomAsync(int roomId, string token)
        {
            var req = new RestRequest($"room/{roomId}", Method.Delete)
                .AddHeader("Accept", "*/*")
                .AddHeader("Referer", "")
                .AddHeader("Cookie", $"token={token}");

            var resp = await _client.ExecuteAsync(req);

            // optional debug logging
            System.Console.WriteLine($"[DELETE /room/{roomId}] Status: {(int)resp.StatusCode} - {resp.StatusCode}");
            System.Console.WriteLine($"[DELETE /room/{roomId}] Body: {resp.Content}");

            return resp;
        }
        public async Task<RestResponse> PutRoomAsync(int roomId, object payload, string token)
        {
            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var req = new RestRequest($"room/{roomId}", Method.Put)
                .AddHeader("Accept", "/")
                .AddHeader("Content-Type", "application/json")
                .AddHeader("Referer", "")
                .AddHeader("Cookie", $"token={token}")
                .AddStringBody(json, "application/json");
            var resp = await _client.ExecuteAsync(req);

            System.Console.WriteLine($"[PUT /room/{roomId}] Status: {(int)resp.StatusCode} - {resp.StatusCode}");
            System.Console.WriteLine($"[PUT /room/{roomId}] Body: {resp.Content}");

            return resp;

        }
    }
}