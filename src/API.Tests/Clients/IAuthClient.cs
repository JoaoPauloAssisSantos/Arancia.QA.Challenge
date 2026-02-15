namespace Arancia.Test.API.Clients
{
    public interface IAuthClient
    {
        Task <string> GetTokenAsync(string username, string password);
    }
}