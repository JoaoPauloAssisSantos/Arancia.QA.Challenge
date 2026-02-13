using Xunit;
using FluentAssertions;
using RestSharp;
public class PingTests
{
    [Fact]
    public async Task Ping_ShouldReturn201()
    {
        var client = ApiClientFactory.Create(Settings.ApiBaseUrl);
        var req = new RestRequest("ping", Method.Get);
        var resp = await client.ExecuteAsync(req);
        resp.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        resp.Content.Should().NotBeNullOrEmpty();
    }
}
