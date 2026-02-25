using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Core.AI
{
    /// <summary>
    /// Routes AI completions to Claude, ChatGPT, or Ollama based on AiSettings.Provider.
    /// All HTTP calls share one HttpClient instance to avoid socket exhaustion.
    /// </summary>
    public sealed class AiService : IAiService
    {
        private static readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(60)
        };

        private static readonly JsonSerializerOptions _json =
            new() { PropertyNameCaseInsensitive = true };

        private AiSettings _settings;

        public AiService(AiSettings settings) => _settings = settings;

        public AiSettings CurrentSettings => _settings;

        public void ApplySettings(AiSettings settings) => _settings = settings;

        // ── Public entry point ───────────────────────────────────────────────

        public Task<AiResult> CompleteAsync(string prompt, string? systemPrompt = null,
                                             CancellationToken ct = default)
            => _settings.Provider switch
            {
                AiProvider.Claude  => CallClaudeAsync(prompt, systemPrompt, ct),
                AiProvider.ChatGpt => CallOpenAiAsync(prompt, systemPrompt, ct),
                AiProvider.Ollama  => CallOllamaAsync(prompt, systemPrompt, ct),
                _ => Task.FromResult(new AiResult
                {
                    Success  = false,
                    Error    = "No AI provider configured. Open Settings → AI to set one up.",
                    Provider = "None"
                })
            };

        // ── Claude ───────────────────────────────────────────────────────────

        private async Task<AiResult> CallClaudeAsync(string prompt, string? system,
                                                       CancellationToken ct)
        {
            const string url = "https://api.anthropic.com/v1/messages";
            var model = string.IsNullOrWhiteSpace(_settings.ClaudeModel)
                ? "claude-opus-4-6" : _settings.ClaudeModel;

            var messages = new[]
            {
                new { role = "user", content = prompt }
            };

            var body = new
            {
                model,
                max_tokens = _settings.MaxTokens,
                system     = system ?? string.Empty,
                messages
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Add("x-api-key",         _settings.ClaudeApiKey);
            req.Headers.Add("anthropic-version",  "2023-06-01");
            req.Content = Json(body);

            try
            {
                using var resp = await _http.SendAsync(req, ct);
                var raw  = await resp.Content.ReadAsStringAsync(ct);

                if (!resp.IsSuccessStatusCode)
                    return Fail("Claude", model, $"HTTP {(int)resp.StatusCode}: {raw}");

                using var doc = JsonDocument.Parse(raw);
                var text = doc.RootElement
                    .GetProperty("content")[0]
                    .GetProperty("text")
                    .GetString() ?? string.Empty;

                return Ok("Claude", model, text);
            }
            catch (Exception ex) { return Fail("Claude", model, ex.Message); }
        }

        // ── ChatGPT / OpenAI ─────────────────────────────────────────────────

        private async Task<AiResult> CallOpenAiAsync(string prompt, string? system,
                                                       CancellationToken ct)
        {
            const string url = "https://api.openai.com/v1/chat/completions";
            var model = string.IsNullOrWhiteSpace(_settings.OpenAiModel)
                ? "gpt-4o" : _settings.OpenAiModel;

            var msgs = new List<object>();
            if (!string.IsNullOrWhiteSpace(system))
                msgs.Add(new { role = "system", content = system });
            msgs.Add(new { role = "user", content = prompt });

            var body = new
            {
                model,
                max_tokens  = _settings.MaxTokens,
                temperature = _settings.Temperature,
                messages    = msgs
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.OpenAiApiKey);
            req.Content = Json(body);

            try
            {
                using var resp = await _http.SendAsync(req, ct);
                var raw = await resp.Content.ReadAsStringAsync(ct);

                if (!resp.IsSuccessStatusCode)
                    return Fail("ChatGPT", model, $"HTTP {(int)resp.StatusCode}: {raw}");

                using var doc = JsonDocument.Parse(raw);
                var text = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? string.Empty;

                return Ok("ChatGPT", model, text);
            }
            catch (Exception ex) { return Fail("ChatGPT", model, ex.Message); }
        }

        // ── Ollama ───────────────────────────────────────────────────────────

        private async Task<AiResult> CallOllamaAsync(string prompt, string? system,
                                                       CancellationToken ct)
        {
            var base_ = _settings.OllamaBaseUrl.TrimEnd('/');
            var model = _settings.OllamaModel;

            if (string.IsNullOrWhiteSpace(model))
                return Fail("Ollama", model, "No Ollama model selected. Open Settings → AI → Ollama.");

            // Use /api/chat for system-prompt support
            var msgs = new List<object>();
            if (!string.IsNullOrWhiteSpace(system))
                msgs.Add(new { role = "system", content = system });
            msgs.Add(new { role = "user", content = prompt });

            var body = new
            {
                model,
                stream   = false,
                options  = new { temperature = _settings.Temperature },
                messages = msgs
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, $"{base_}/api/chat");
            req.Content = Json(body);

            try
            {
                using var resp = await _http.SendAsync(req, ct);
                var raw = await resp.Content.ReadAsStringAsync(ct);

                if (!resp.IsSuccessStatusCode)
                    return Fail("Ollama", model, $"HTTP {(int)resp.StatusCode}: {raw}");

                using var doc = JsonDocument.Parse(raw);
                var text = doc.RootElement
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? string.Empty;

                return Ok("Ollama", model, text);
            }
            catch (Exception ex) { return Fail("Ollama", model, ex.Message); }
        }

        // ── Ollama probe (auto-detect + model list) ───────────────────────────

        public async Task<OllamaProbeResult> ProbeOllamaAsync(string baseUrl,
                                                               CancellationToken ct = default)
        {
            var base_ = baseUrl.TrimEnd('/');
            try
            {
                using var resp = await _http.GetAsync($"{base_}/api/tags", ct);
                if (!resp.IsSuccessStatusCode)
                    return new OllamaProbeResult
                    {
                        IsAvailable = false,
                        Error = $"Ollama responded HTTP {(int)resp.StatusCode}"
                    };

                var raw = await resp.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(raw);

                var models = doc.RootElement
                    .GetProperty("models")
                    .EnumerateArray()
                    .Select(m => m.GetProperty("name").GetString() ?? string.Empty)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .OrderBy(n => n)
                    .ToList();

                return new OllamaProbeResult { IsAvailable = true, Models = models };
            }
            catch (HttpRequestException ex)
            {
                return new OllamaProbeResult
                {
                    IsAvailable = false,
                    Error = $"Cannot reach Ollama at {base_}. Is it running? ({ex.Message})"
                };
            }
            catch (Exception ex)
            {
                return new OllamaProbeResult { IsAvailable = false, Error = ex.Message };
            }
        }

        // ── Provider self-test ────────────────────────────────────────────────

        public async Task<(bool Ok, string Message)> TestProviderAsync(CancellationToken ct = default)
        {
            var result = await CompleteAsync("Say only: OK", "You are a test.", ct);
            if (result.Success)
                return (true, $"✓ {result.Provider} ({result.Model}) responded successfully.");
            return (false, $"✗ {result.Error}");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static StringContent Json(object body)
            => new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        private static AiResult Ok(string provider, string model, string text)
            => new() { Success = true, Provider = provider, Model = model, Text = text };

        private static AiResult Fail(string provider, string model, string error)
            => new() { Success = false, Provider = provider, Model = model, Error = error };
    }
}
