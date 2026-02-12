using RestSharp;
public static class ApiClientFactory
{
    public static RestClient Create(string baseUrl)
    {
        var options = new RestClientOptions(baseUrl) { ThrowOnAnyError = false };
        return new RestClient(options);
    }
}
