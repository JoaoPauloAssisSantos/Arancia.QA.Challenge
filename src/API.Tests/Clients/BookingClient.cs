using Arancia.Test.API.Helpers;
using RestSharp;
using System.Text.Json;
namespace Arancia.Test.API.Clients;

public class BookingClient
{
    private readonly RestClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    // Default ctor uses AutomationTesting API base (keeps previous behavior)
    public BookingClient() =>
        _client = ApiClientFactory.Create(Settings.AutomationTestingApiBase);

    // New ctor for dependency injection / different base (e.g., Restful-Booker)
    public BookingClient(RestClient client) =>
        _client = client ?? throw new ArgumentNullException(nameof(client));

    public async Task<RestResponse> CreateBookingAsync(Booking booking)
    {
        Console.WriteLine("Enter CreateBookingAsync");

        var json = JsonSerializer.Serialize(booking, _jsonOptions);
        Console.WriteLine($"Request JSON: {json}");

        var req = new RestRequest("booking", Method.Post);
        req.AddHeader("Accept", "application/json");
        req.AddHeader("Content-Type", "application/json");
        req.AddStringBody(json, "application/json");

        var resp = await _client.ExecuteAsync(req);

        Console.WriteLine($"Received status: {(int)resp.StatusCode} - {resp.StatusCode}");
        Console.WriteLine($"Response body: {resp.Content}");

        return resp;
    }

    public async Task<RestResponse> GetBookingAsync(int id)
    {
        var req = new RestRequest($"booking/{id}", Method.Get);
        return await _client.ExecuteAsync(req);
    }

    public async Task<RestResponse> DeleteBookingAsync(int id, string token)
    {
        var req = new RestRequest($"booking/{id}", Method.Delete)
            .AddHeader("Accept", "application/json")
            .AddHeader("Cookie", $"token={token}");

        var resp = await _client.ExecuteAsync(req);

        Console.WriteLine($"[DELETE] Status: {(int)resp.StatusCode} - {resp.StatusCode}");
        Console.WriteLine($"[DELETE] Body  : {resp.Content}");

        return resp;
    }

    public async Task<RestResponse> PatchBookingFirstnameAsync(int id, string newFirstname, string token)
    {
        var req = new RestRequest($"booking/{id}", Method.Patch)
            .AddHeader("Accept", "application/json")
            .AddHeader("Content-Type", "application/json")
            .AddHeader("Cookie", $"token={token}")
            .AddJsonBody(new { firstname = newFirstname });

        return await _client.ExecuteAsync(req);
    }
}
