namespace Core.AI
{
    /// <summary>Result of an AI completion call.</summary>
    public sealed class AiResult
    {
        public bool   Success  { get; init; }
        public string Text     { get; init; } = string.Empty;
        public string Error    { get; init; } = string.Empty;
        public string Provider { get; init; } = string.Empty;
        public string Model    { get; init; } = string.Empty;
    }

    /// <summary>Result of an Ollama model-list / connectivity probe.</summary>
    public sealed class OllamaProbeResult
    {
        public bool          IsAvailable { get; init; }
        public List<string>  Models      { get; init; } = new();
        public string        Error       { get; init; } = string.Empty;
    }

    public interface IAiService
    {
        /// <summary>Send a plain-text prompt and get a completion.</summary>
        Task<AiResult> CompleteAsync(string prompt, string? systemPrompt = null,
                                     CancellationToken ct = default);

        /// <summary>
        /// Probe Ollama at the configured URL: checks if it is running and
        /// returns all locally pulled models.
        /// </summary>
        Task<OllamaProbeResult> ProbeOllamaAsync(string baseUrl,
                                                  CancellationToken ct = default);

        /// <summary>Test the currently active provider. Returns (success, message).</summary>
        Task<(bool Ok, string Message)> TestProviderAsync(CancellationToken ct = default);

        /// <summary>Live settings — update provider/keys without restarting.</summary>
        void ApplySettings(AiSettings settings);

        AiSettings CurrentSettings { get; }
    }
}
