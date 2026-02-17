namespace Arancia.Test.API.Clients
{
    public interface IAuthClient
    {
        Task <string> GetTokenAsync(string username, string password);
        /// <summary>
        /// Invalidates/destroys a previously issued token.
        /// </summary>
        Task DestroyTokenAsync(string token);
    }
}