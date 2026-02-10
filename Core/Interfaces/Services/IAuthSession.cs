namespace Core.Interfaces.Services
{
    public interface IAuthSession
    {
        string? Token { get; }
        bool IsAuthenticated { get; }
        void SetToken(string token);
        void Clear();
    }
}
