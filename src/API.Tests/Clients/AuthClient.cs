using System.Net;
using System.Text.Json;
using FluentAssertions;
using RestSharp;

public class AuthClient
{
    private readonly RestClient _client;
    private readonly JsonSerializerOptions _jsonOptions =
        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public AuthClient() =>
        _client = ApiClientFactory.Create(Settings.ApiBaseUrl);

    public async Task<string> GetTokenAsync(string username = "admin", string password = "password123")
    {
        var body = new { username, password };

        var req = new RestRequest("auth", Method.Post)
            .AddHeader("Accept", "application/json")
            .AddJsonBody(body);

        var resp = await _client.ExecuteAsync(req);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Should().NotBeNullOrEmpty();

        using var doc = JsonDocument.Parse(resp.Content!);
        doc.RootElement.TryGetProperty("token", out var tokenEl)
            .Should().BeTrue("auth response must contain token");

        var token = tokenEl.GetString();
        token.Should().NotBeNullOrWhiteSpace();

        return token!;
    }
}
