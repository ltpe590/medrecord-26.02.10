namespace Core.Models
{
    public class ConnectionTestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? StatusCode { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public DateTime TestedAt { get; set; }
        public string Endpoint { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
    }

    public class ConnectionStatusChangedEventArgs : EventArgs
    {
        public bool IsConnected { get; }
        public string Message { get; }

        public ConnectionStatusChangedEventArgs(bool isConnected, string? message = null)
        {
            IsConnected = isConnected;
            Message = message ?? string.Empty;
        }
    }
}
