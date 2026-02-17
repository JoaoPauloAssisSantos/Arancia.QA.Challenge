using System.Net;
using System.Text.Json;
using Arancia.Test.API.Helpers;
using RestSharp;
namespace Arancia.Test.API.Clients;

public class RestfulBookerAuthClient : IAuthClient
{
    private readonly RestClient _client;
    public RestfulBookerAuthClient(RestClient? client = null) =>
    _client = client ?? ApiClientFactory.Create(Settings.RestfulBookerBaseUrl);

    public async Task<string> GetTokenAsync(string username, string password)
    {
        var req = new RestRequest("auth", Method.Post)
            .AddHeader("Accept", "application/json")
            .AddJsonBody(new { username, password });

        var resp = await _client.ExecuteAsync(req);

        if (resp.StatusCode != HttpStatusCode.OK || string.IsNullOrWhiteSpace(resp.Content))
            throw new HttpRequestException($"Auth failed. Status: {(int)resp.StatusCode}. Body: {resp.Content}");

        using var doc = JsonDocument.Parse(resp.Content);
        if (!doc.RootElement.TryGetProperty("token", out var tokenEl) || tokenEl.ValueKind != JsonValueKind.String)
            throw new InvalidOperationException($"Auth response missing token. Body: {resp.Content}");

        return tokenEl.GetString()!;
    }
    public async Task DestroyTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token must be non-empty.", nameof(token));

        // Endpoint: POST /api/auth/logout
        var req = new RestRequest("auth/logout", Method.Post)
            .AddHeader("Accept", "*/*")
            .AddHeader("Content-Type", "application/json")
            .AddJsonBody(new { token });

        var resp = await _client.ExecuteAsync(req);

        // Accept common success statuses (tune based on your API’s behavior)
        if (resp.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent)
            return;

        // If the API returns something else but you still consider logout “best-effort”,
        // you might log and return instead of throwing. For now, be strict:
        throw new HttpRequestException(
            $"DestroyTokenAsync failed on AutomationTestingAuthClient. Status: {(int)resp.StatusCode}. Body: {resp.Content}");
    }
}