namespace Core.Models
{
    /// <summary>Immutable result of a connection test. Set all properties at construction via object initializer.</summary>
    public class ConnectionTestResult
    {
        public bool      Success      { get; init; }
        public string    Message      { get; init; } = string.Empty;
        public int?      StatusCode   { get; init; }
        public TimeSpan  ResponseTime { get; init; }
        public DateTime  TestedAt     { get; init; }
        public string    Endpoint     { get; init; } = string.Empty;
        public Exception? Exception   { get; init; }
    }

    public class ConnectionStatusChangedEventArgs : EventArgs
    {
        public bool   IsConnected { get; }
        public string Message     { get; }

        public ConnectionStatusChangedEventArgs(bool isConnected, string? message = null)
        {
            IsConnected = isConnected;
            Message     = message ?? string.Empty;
        }
    }
}
