namespace Core.Interfaces.Services
{
    public interface ILoginService
    {
        Task<string> LoginAsync(string username, string password, string baseUrl, CancellationToken ct = default);
    }
}