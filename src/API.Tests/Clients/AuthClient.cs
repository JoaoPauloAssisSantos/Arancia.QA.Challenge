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
    /// <summary>
    /// Destroys / invalidates an existing token.
    /// Expected endpoint: POST /api/auth/logout with JSON body { "token": "abc123" }.
    /// </summary>
    public async Task DestroyTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token must be non-empty", nameof(token));

        var req = new RestRequest("api/auth/logout", Method.Post)
            .AddHeader("Accept", "*/*")
            .AddHeader("Content-Type", "application/json")
            .AddJsonBody(new { token });

        var resp = await _client.ExecuteAsync(req);

        // accept 200 OK, 204 NoContent (or whatever the API returns for successful logout)
        if (resp.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent)
            return;

        // if the API uses a different "success" code (e.g. 202), add it above
        throw new HttpRequestException(
            $"DestroyToken failed. Status: {(int)resp.StatusCode}. Body: {resp.Content}");
    }
}
