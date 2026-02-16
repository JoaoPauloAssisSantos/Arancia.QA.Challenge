using Xunit;
using FluentAssertions;
using RestSharp;
using Arancia.Test.API.Helpers;
public class PingTests
{
    [Fact(DisplayName = "API-01 - Health Check (Ping)")]
    public async Task Ping_ShouldReturn201()
    {
        var client = ApiClientFactory.Create(Settings.RestfulBookerBaseUrl);
        var req = new RestRequest("ping", Method.Get);
        var resp = await client.ExecuteAsync(req);
        resp.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        resp.Content.Should().NotBeNullOrEmpty();
    }
}
