using RestSharp;
using System.Threading.Tasks;

public class BookingClient
{
    private readonly RestClient _client;
    public BookingClient() => _client = ApiClientFactory.Create(Settings.ApiBaseUrl);

    public async Task<RestResponse> CreateBookingAsync(Booking booking)
    {
        Console.WriteLine("Enter CreateBookingAsync");
        var options = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };
        var json = System.Text.Json.JsonSerializer.Serialize(booking, options);
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
}