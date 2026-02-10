namespace Core.Interfaces.Services
{
    public interface IApiService
    {
        void SetAuthToken(string token);
        Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default);
        Task PutAsync<TRequest>(string endpoint, TRequest data, CancellationToken ct = default);
        Task DeleteAsync(string endpoint, CancellationToken ct = default);
        Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data, CancellationToken ct = default);
    }
}
