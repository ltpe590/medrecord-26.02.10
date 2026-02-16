using Core.Interfaces.Services;

namespace Core.Services
{
    public class AuthSession : IAuthSession
    {
        public string? Token { get; private set; }
        public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Token);

        public void SetToken(string token) => Token = token;

        public void Clear() => Token = null;
    }
}