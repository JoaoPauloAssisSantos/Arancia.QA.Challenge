using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Arancia.Test.API.Helpers;
using RestSharp;

namespace Arancia.Test.API.Clients
{
    public class BookingApiHelper
    {
        private readonly RestClient _client;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        public BookingApiHelper(RestClient? client = null)
        {
            _client = client ?? ApiClientFactory.Create(Settings.AutomationTestingApiBase);
        }

        // Performs PUT /booking/{id} with the provided payload (object will be serialized to JSON).
        // Returns the raw RestResponse for assertions.
        public async Task<RestResponse> PutBookingAsync(int bookingId, object payload, string token)
        {
            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var req = new RestRequest($"booking/{bookingId}", Method.Put)
                .AddHeader("Referer", "")
                .AddHeader("Content-Type", "application/json")
                .AddHeader("Cookie", $"token={token}")
                .AddStringBody(json, "application/json");

            return await _client.ExecuteAsync(req);
        }

        // Performs GET /booking/{id} and returns the raw RestResponse.
        public async Task<RestResponse> GetBookingRawAsync(int bookingId, string? token = null)
        {
            var req = new RestRequest($"booking/{bookingId}", Method.Get)
                .AddHeader("Accept", "*/*")
                .AddHeader("Referer", "");

            if (!string.IsNullOrWhiteSpace(token))
                req.AddHeader("Cookie", $"token={token}");

            return await _client.ExecuteAsync(req);
        }

        // Performs GET and deserializes the response to Booking (throws on non-200 or malformed JSON)
        public async Task<Booking> GetBookingAsync(int bookingId, string? token = null)
        {
            var resp = await GetBookingRawAsync(bookingId, token);
            if (resp.StatusCode != HttpStatusCode.OK)
                throw new HttpRequestException($"GET booking/{bookingId} failed. Status: {(int)resp.StatusCode}. Body: {resp.Content}");

            if (string.IsNullOrWhiteSpace(resp.Content))
                throw new InvalidOperationException("GET booking returned empty body.");

            var booking = JsonSerializer.Deserialize<Booking>(resp.Content!, _jsonOptions);
            if (booking is null)
                throw new InvalidOperationException("Failed to deserialize booking response.");

            return booking;
        }
    }
}