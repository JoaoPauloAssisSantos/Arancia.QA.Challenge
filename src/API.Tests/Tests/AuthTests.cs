using System.Text.Json;
using Arancia.Test.API.Helpers;
using FluentAssertions;
using RestSharp;
using Xunit.Abstractions;

public class AuthTests : TestBase
{
    public AuthTests(ITestOutputHelper output)
    {
        InitTestBase(output); // sets Output and AuthClient
    }
    [Fact(DisplayName = "API-08 - Authentication success returns token")]
    public async Task Auth_WithValidCredentials_ReturnsToken()
    {
        string? token = null;

        try
        {
            // Act
            token = await AuthClient.GetTokenAsync("admin", "password123");

            // Log + assert
            Output.WriteLine($"Token: {token}");
            token.Should().NotBeNullOrWhiteSpace("token must be returned for valid credentials");
        }
        finally
        {
            // Teardown: destroy the token if it was successfully obtained
            if (!string.IsNullOrWhiteSpace(token))
            {
                try
                {
                    await AuthClient.DestroyTokenAsync(token!);
                    Output.WriteLine("Token destroyed successfully after auth test.");
                }
                catch (Exception ex)
                {
                    Output.WriteLine($"Failed to destroy token after auth test: {ex.Message}");
                }
            }
        }
    }

    [Fact(DisplayName = "API-09 - Authentication with invalid credentials does not return token")]
    public async Task Auth_WithInvalidCredentials_DoesNotReturnToken()
    {
        // Arrange: call raw endpoint to assert observed behavior for invalid creds
        var client = ApiClientFactory.Create(Settings.RestfulBookerBaseUrl);
        var body = new { username = "admin", password = "wrong-pass" };
        var req = new RestRequest("auth", Method.Post)
            .AddHeader("Accept", "application/json")
            .AddJsonBody(body);

        // Act
        var resp = await client.ExecuteAsync(req);

        // Log
        Output.WriteLine($"Status: {(int)resp.StatusCode} - {resp.StatusCode}");
        Output.WriteLine($"Body  : {resp.Content}");

        // Assert: observed behavior is 200 with reason and no token
        resp.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        resp.Content.Should().NotBeNullOrWhiteSpace();

        using var doc = JsonDocument.Parse(resp.Content!);
        var root = doc.RootElement;

        root.TryGetProperty("token", out _).Should().BeFalse("invalid credentials must not return a token");
        root.TryGetProperty("reason", out var reasonEl).Should().BeTrue("response should include a reason for failure");
        reasonEl.GetString().Should().Be("Bad credentials");
    }
}