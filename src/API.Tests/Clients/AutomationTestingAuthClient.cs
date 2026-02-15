using System;
using System.Net;
using System.Text.Json;
using RestSharp;

namespace Arancia.Test.API.Clients
{
    public class AutomationTestingAuthClient : IAuthClient
    {
        private readonly RestClient _client;
        public AutomationTestingAuthClient(RestClient? client = null) =>
    _client = client ?? ApiClientFactory.Create(Settings.AutomationTestingApiBase); // should be "https://automationintesting.online/api"

        public async Task<string> GetTokenAsync(string username, string password)
        {
            var req = new RestRequest("auth/login", Method.Post)
                .AddHeader("Accept", "application/json")
                .AddHeader("Content-Type", "application/json")
                .AddJsonBody(new { username, password });

            var resp = await _client.ExecuteAsync(req);

            if (resp.StatusCode != HttpStatusCode.OK || string.IsNullOrWhiteSpace(resp.Content))
                throw new HttpRequestException($"Auth failed. Status: {(int)resp.StatusCode}. Body: {resp.Content}");

            using var doc = JsonDocument.Parse(resp.Content);
            if (!doc.RootElement.TryGetProperty("token", out var tokenEl) || tokenEl.ValueKind != JsonValueKind.String)
                throw new InvalidOperationException($"Auth response missing token. Body: {resp.Content}");

            return tokenEl.GetString()!;
        }
    }
}