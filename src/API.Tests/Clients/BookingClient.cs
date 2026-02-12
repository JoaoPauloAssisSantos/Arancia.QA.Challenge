using RestSharp;
using System.Threading.Tasks;
public class BookingClient
{
    private readonly RestClient _client;
    public BookingClient() => _client = ApiClientFactory.Create(Settings.ApiBaseUrl);public async Task<RestResponse> CreateBookingAsync(Booking booking)
{
    var req = new RestRequest("booking", Method.Post).AddJsonBody(booking);
    return await _client.ExecuteAsync(req);
}

public async Task<RestResponse> GetBookingAsync(int id)
{
    var req = new RestRequest($"booking/{id}", Method.Get);
    return await _client.ExecuteAsync(req);
}}
