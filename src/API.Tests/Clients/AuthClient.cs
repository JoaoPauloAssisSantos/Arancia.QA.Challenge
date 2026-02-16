using System.Net;
using System.Text.Json;
using Arancia.Test.API.Helpers;
using RestSharp;
namespace Arancia.Test.API.Clients;

public class AuthClient
{
    private readonly RestClient _client;
    public AuthClient(RestClient? client = null) =>
    _client = client ?? ApiClientFactory.Create(Settings.RestfulBookerBaseUrl);

    public async Task<string> GetTokenAsync(string username = "admin", string password = "password123")
    {
        var req = new RestRequest("auth", Method.Post)
            .AddHeader("Accept", "application/json")
            .AddJsonBody(new { username, password });

        var resp = await _client.ExecuteAsync(req);

        if (resp.StatusCode != HttpStatusCode.OK || string.IsNullOrWhiteSpace(resp.Content))
            throw new HttpRequestException($"Auth failed. Status: {(int)resp.StatusCode}. Body: {resp.Content}");

        using var doc = JsonDocument.Parse(resp.Content);
        if (!doc.RootElement.TryGetProperty("token", out var tokenEl) || tokenEl.ValueKind == JsonValueKind.Null)
            throw new InvalidOperationException($"Auth response missing token. Body: {resp.Content}");

        return tokenEl.GetString()!;
    }
}
