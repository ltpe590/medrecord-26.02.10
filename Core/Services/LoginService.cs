using Core.DTOs;
using Core.Interfaces.Services;

public sealed class LoginService : ILoginService
{
    private readonly IApiService _apiService;
    private readonly IAuthSession _authSession;

    public LoginService(IApiService apiService, IAuthSession authSession)
    {
        _apiService = apiService;
        _authSession = authSession;
    }

    public async Task<string> LoginAsync(string username, string password, string baseUrl, CancellationToken ct = default)
    {
        var request = new { Username = username, Password = password };
        var response = await _apiService.PostAsync<object, AuthResponse>("api/auth/login", request, ct);

        if (!string.IsNullOrWhiteSpace(response?.Token))
        {
            _authSession.SetToken(response.Token);
        }

        return response?.Token ?? string.Empty;
    }
}