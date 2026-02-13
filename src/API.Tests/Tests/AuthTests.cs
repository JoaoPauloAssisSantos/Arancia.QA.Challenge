using System.Text.Json;
using FluentAssertions;
using RestSharp;
using Xunit.Abstractions;

public class AuthTests : TestBase
{
    private readonly ITestOutputHelper _output;

    public AuthTests(ITestOutputHelper output) => _output = output;

    [Fact(DisplayName = "API-08 - Authentication success returns token")]
    public async Task Auth_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var client = ApiClientFactory.Create(Settings.ApiBaseUrl);
        var body = new
        {
            username = "admin",
            password = "password123"
        };

        // Act
        var req = new RestRequest("auth", Method.Post)
            .AddHeader("Accept", "application/json")
            .AddJsonBody(body);

        var resp = await client.ExecuteAsync(req);

        // Log
        _output.WriteLine($"Status: {(int)resp.StatusCode} - {resp.StatusCode}");
        _output.WriteLine($"Body  : {resp.Content}");

        // Assert
        resp.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        resp.Content.Should().NotBeNullOrEmpty();

        using var doc = JsonDocument.Parse(resp.Content!);
        doc.RootElement.TryGetProperty("token", out var tokenEl).Should().BeTrue("auth success must return token");
        var token = tokenEl.GetString();

        token.Should().NotBeNullOrWhiteSpace("token must be non-empty on successful authentication");
    }

    [Fact(DisplayName = "API-09 - Authentication with invalid credentials does not return token")]
    public async Task Auth_WithInvalidCredentials_DoesNotReturnToken()
    {
        // Arrange
        var client = ApiClientFactory.Create(Settings.ApiBaseUrl);
        var body = new
        {
            username = "admin",
            password = "wrong-pass"
        };

        // Act
        var req = new RestRequest("auth", Method.Post)
            .AddHeader("Accept", "application/json")
            .AddJsonBody(body);

        var resp = await client.ExecuteAsync(req);

        // Log
        _output.WriteLine($"Status: {(int)resp.StatusCode} - {resp.StatusCode}");
        _output.WriteLine($"Body  : {resp.Content}");

        // Assert
        resp.Content.Should().NotBeNull();

        if (!string.IsNullOrWhiteSpace(resp.Content) &&
            resp.Content!.TrimStart().StartsWith("{"))
        {
            using var doc = JsonDocument.Parse(resp.Content);
            var hasToken = doc.RootElement.TryGetProperty("token", out var tokenEl)
                           && tokenEl.ValueKind != JsonValueKind.Null
                           && !string.IsNullOrWhiteSpace(tokenEl.GetString());

            hasToken.Should().BeFalse("invalid credentials must not return a token");
        }
    }
}
