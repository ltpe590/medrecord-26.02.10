using Core.Models;

namespace Core.Interfaces.Services
{
    public interface IConnectionService
    {
        Task<ConnectionTestResult> TestApiConnectionAsync(string apiUrl);

        bool IsApiConnected { get; }

        event EventHandler<ConnectionStatusChangedEventArgs> ConnectionStatusChanged;
    }
}