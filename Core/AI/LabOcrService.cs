using System.Diagnostics;
using Core.DTOs;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core.AI
{
    /// <summary>
    /// Extracts structured lab result data from images using the active AI provider.
    /// Supports Claude, ChatGPT, and Ollama vision models.
    /// </summary>
    public static class LabOcrService
    {
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(60) };

        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull
        };

        private const string OcrPrompt =
            "This is a medical lab result image. " +
            "Extract the FIRST test result you can identify and return ONLY valid JSON " +
            "with exactly these keys: testName, resultValue, unit, normalRange. " +
            "If a field is not visible, use an empty string. " +
            "Example: {\"testName\":\"Glucose\",\"resultValue\":\"5.2\",\"unit\":\"mmol/L\",\"normalRange\":\"3.9-6.1\"} " +
            "Return JSON only, no explanation, no markdown fences.";

        // ── Public entry point ────────────────────────────────────────────────

        public static async Task<LabOcrResult?> ExtractAsync(
            string base64Image,
            string mimeType,
            IAiService? aiService = null)
        {
            var provider = aiService?.CurrentSettings.Provider ?? AiProvider.None;

            return provider switch
            {
                AiProvider.Claude  => await CallClaudeAsync(
                                          aiService!.CurrentSettings.ClaudeApiKey,
                                          aiService.CurrentSettings.ClaudeModel,
                                          base64Image, mimeType),

                AiProvider.ChatGpt => await CallOpenAiAsync(
                                          aiService!.CurrentSettings.OpenAiApiKey,
                                          aiService.CurrentSettings.OpenAiModel,
                                          base64Image, mimeType),

                AiProvider.Ollama  => await CallOllamaAsync(
                                          aiService!.CurrentSettings.OllamaBaseUrl,
                                          aiService.CurrentSettings.OllamaModel,
                                          base64Image, mimeType),

                _                  => await TryEnvFallbackAsync(base64Image, mimeType)
            };
        }

        // ── Claude vision ─────────────────────────────────────────────────────

        private static async Task<LabOcrResult?> CallClaudeAsync(
            string apiKey, string model, string base64Image, string mimeType)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) return null;
            if (string.IsNullOrWhiteSpace(model)) model = "claude-opus-4-6";

            var body = new
            {
                model,
                max_tokens = 300,
                messages = new[]
                {
                    new
                    {
                        role    = "user",
                        content = new object[]
                        {
                            new { type = "image", source = new { type = "base64", media_type = mimeType, data = base64Image } },
                            new { type = "text",  text   = OcrPrompt }
                        }
                    }
                }
            };

            var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            req.Headers.Add("x-api-key",         apiKey);
            req.Headers.Add("anthropic-version", "2023-06-01");
            req.Content = Serialize(body);

            try
            {
                var resp = await _http.SendAsync(req);
                resp.EnsureSuccessStatusCode();
                var raw  = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(raw);
                var text = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "";
                return ParseLabJson(text);
            }
            catch (Exception ex) { Debug.WriteLine($"[LabOcrService] Claude OCR failed: {ex.Message}"); return null; }
        }

        // ── OpenAI vision ─────────────────────────────────────────────────────

        private static async Task<LabOcrResult?> CallOpenAiAsync(
            string apiKey, string model, string base64Image, string mimeType)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) return null;
            if (string.IsNullOrWhiteSpace(model)) model = "gpt-4o";

            var body = new
            {
                model,
                max_tokens = 300,
                messages = new object[]
                {
                    new
                    {
                        role    = "user",
                        content = new object[]
                        {
                            new { type = "image_url", image_url = new { url = $"data:{mimeType};base64,{base64Image}" } },
                            new { type = "text", text = OcrPrompt }
                        }
                    }
                }
            };

            var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            req.Content = Serialize(body);

            try
            {
                var resp = await _http.SendAsync(req);
                resp.EnsureSuccessStatusCode();
                var raw  = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(raw);
                var text = doc.RootElement
                    .GetProperty("choices")[0].GetProperty("message").GetProperty("content")
                    .GetString() ?? "";
                return ParseLabJson(text);
            }
            catch (Exception ex) { Debug.WriteLine($"[LabOcrService] OpenAI OCR failed: {ex.Message}"); return null; }
        }

        // ── Ollama vision ─────────────────────────────────────────────────────

        private static async Task<LabOcrResult?> CallOllamaAsync(
            string baseUrl, string model, string base64Image, string mimeType)
        {
            if (string.IsNullOrWhiteSpace(model)) return null;
            baseUrl = baseUrl.TrimEnd('/');

            var body = new
            {
                model,
                stream = false,
                prompt = OcrPrompt,
                images = new[] { base64Image }
            };

            var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/generate");
            req.Content = Serialize(body);

            try
            {
                var resp = await _http.SendAsync(req);
                resp.EnsureSuccessStatusCode();
                var raw  = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(raw);
                var text = doc.RootElement.GetProperty("response").GetString() ?? "";
                return ParseLabJson(text);
            }
            catch (Exception ex) { Debug.WriteLine($"[LabOcrService] Ollama OCR failed: {ex.Message}"); return null; }
        }

        // ── Env-var fallback ──────────────────────────────────────────────────

        private static async Task<LabOcrResult?> TryEnvFallbackAsync(string base64Image, string mimeType)
        {
            var key = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(key)) return null;
            return await CallClaudeAsync(key, "claude-opus-4-6", base64Image, mimeType);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static StringContent Serialize(object body)
            => new(JsonSerializer.Serialize(body, _json), Encoding.UTF8, "application/json");

        private static LabOcrResult? ParseLabJson(string raw)
        {
            raw = raw.Trim();
            if (raw.StartsWith("```"))
            {
                var lines = raw.Split('\n');
                raw = string.Join('\n', lines.Skip(1).TakeWhile(l => !l.TrimStart().StartsWith("```")));
            }
            raw = raw.Trim('`').Trim();
            int start = raw.IndexOf('{');
            int end   = raw.LastIndexOf('}');
            if (start >= 0 && end > start) raw = raw[start..(end + 1)];
            try { return JsonSerializer.Deserialize<LabOcrResult>(raw, _json); }
            catch (Exception ex) { Debug.WriteLine($"[LabOcrService] JSON parse failed: {ex.Message}"); return null; }
        }
    }
}