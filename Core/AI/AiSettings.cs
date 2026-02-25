namespace Core.AI
{
    public enum AiProvider
    {
        None    = 0,
        Claude  = 1,
        ChatGpt = 2,
        Ollama  = 3
    }

    public class AiSettings
    {
        public AiProvider Provider       { get; set; } = AiProvider.None;

        // Claude (Anthropic)
        public string ClaudeApiKey       { get; set; } = string.Empty;
        public string ClaudeModel        { get; set; } = "claude-opus-4-6";

        // ChatGPT (OpenAI)
        public string OpenAiApiKey       { get; set; } = string.Empty;
        public string OpenAiModel        { get; set; } = "gpt-4o";

        // Ollama (local)
        public string OllamaBaseUrl      { get; set; } = "http://localhost:11434";
        public string OllamaModel        { get; set; } = string.Empty;

        /// <summary>Maximum tokens to request in a completion.</summary>
        public int    MaxTokens          { get; set; } = 1024;

        /// <summary>0.0–1.0 — lower = more deterministic (good for medical summaries).</summary>
        public double Temperature        { get; set; } = 0.3;
    }
}
