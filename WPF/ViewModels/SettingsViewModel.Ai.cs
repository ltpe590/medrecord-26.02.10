using Core.AI;
using Core.DTOs;
using Core.Entities;
using Core.Interfaces.Services;
using Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using WPF.Configuration;
using WPF.Services;

namespace WPF.ViewModels
{
    public partial class SettingsViewModel
    {
        #region AI Tab

        // ── Provider selection ────────────────────────────────────────────────
        private string _aiProvider    = "None";
        private bool   _aiIsTestingProvider;
        private string _aiStatus      = string.Empty;
        private string _aiStatusColor = "Gray";

        public static IReadOnlyList<string> AiProviders { get; } =
            new[] { "None", "Claude", "ChatGpt", "Ollama" };

        public string AiProvider
        {
            get => _aiProvider;
            set
            {
                _aiProvider = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowClaudeFields));
                OnPropertyChanged(nameof(ShowOpenAiFields));
                OnPropertyChanged(nameof(ShowOllamaFields));
                OnPropertyChanged(nameof(CanTestAi));
                AiStatus = string.Empty;
            }
        }

        public bool ShowClaudeFields  => _aiProvider == "Claude";
        public bool ShowOpenAiFields  => _aiProvider == "ChatGpt";
        public bool ShowOllamaFields  => _aiProvider == "Ollama";

        public bool AiIsTestingProvider
        {
            get => _aiIsTestingProvider;
            private set { _aiIsTestingProvider = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanTestAi)); }
        }
        public string AiStatus
        {
            get => _aiStatus;
            private set { _aiStatus = value; OnPropertyChanged(); }
        }
        public string AiStatusColor
        {
            get => _aiStatusColor;
            private set { _aiStatusColor = value; OnPropertyChanged(); }
        }
        public bool CanTestAi => !AiIsTestingProvider && _aiProvider != "None";

        // ── Claude fields ─────────────────────────────────────────────────────
        private string _claudeApiKey = string.Empty;
        private string _claudeModel  = "claude-opus-4-5";

        public string ClaudeApiKey
        {
            get => _claudeApiKey;
            set { _claudeApiKey = value; OnPropertyChanged(); }
        }
        public string ClaudeModel
        {
            get => _claudeModel;
            set { _claudeModel = value; OnPropertyChanged(); }
        }

        public static IReadOnlyList<string> ClaudeModels { get; } = new[]
        {
            "claude-opus-4-6",
            "claude-sonnet-4-5",
            "claude-haiku-4-5"
        };

        // ── OpenAI / ChatGPT fields ───────────────────────────────────────────
        private string _openAiApiKey = string.Empty;
        private string _openAiModel  = "gpt-4o";

        public string OpenAiApiKey
        {
            get => _openAiApiKey;
            set { _openAiApiKey = value; OnPropertyChanged(); }
        }
        public string OpenAiModel
        {
            get => _openAiModel;
            set { _openAiModel = value; OnPropertyChanged(); }
        }

        public static IReadOnlyList<string> OpenAiModels { get; } = new[]
        {
            "gpt-4o",
            "gpt-4o-mini",
            "gpt-4-turbo",
            "gpt-3.5-turbo"
        };

        // ── Ollama fields ─────────────────────────────────────────────────────
        private string _ollamaBaseUrl = "http://localhost:11434";
        private string _ollamaModel   = string.Empty;
        private ObservableCollection<string> _ollamaModels = new();
        private bool   _ollamaDetected;
        private string _ollamaDetectStatus = "Not checked";

        public string OllamaBaseUrl
        {
            get => _ollamaBaseUrl;
            set { _ollamaBaseUrl = value; OnPropertyChanged(); }
        }
        public string OllamaModel
        {
            get => _ollamaModel;
            set { _ollamaModel = value; OnPropertyChanged(); }
        }
        public ObservableCollection<string> OllamaModels
        {
            get => _ollamaModels;
            private set { _ollamaModels = value; OnPropertyChanged(); }
        }
        public bool OllamaDetected
        {
            get => _ollamaDetected;
            private set
            {
                _ollamaDetected = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(OllamaStatusColor));
            }
        }
        public string OllamaStatusColor => _ollamaDetected ? "#4CAF50" : "#9E9E9E";
        public string OllamaDetectStatus
        {
            get => _ollamaDetectStatus;
            private set { _ollamaDetectStatus = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Probe Ollama at the configured URL, populate OllamaModels,
        /// and auto-select the first model if none is chosen.
        /// Called on startup and when the user clicks "Detect".
        /// </summary>
        public async Task DetectOllamaAsync()
        {
            OllamaDetectStatus = "Detecting...";
            OllamaDetected     = false;

            var svc  = new AiService(BuildAiSettings());
            var probe = await svc.ProbeOllamaAsync(_ollamaBaseUrl);

            if (probe.IsAvailable)
            {
                OllamaDetected     = true;
                OllamaDetectStatus = $"✓ Ollama running — {probe.Models.Count} model(s) found";
                OllamaModels.Clear();
                foreach (var m in probe.Models) OllamaModels.Add(m);

                // Auto-select if nothing chosen yet
                if (string.IsNullOrWhiteSpace(OllamaModel) && probe.Models.Count > 0)
                    OllamaModel = probe.Models[0];
            }
            else
            {
                OllamaDetected     = false;
                OllamaDetectStatus = $"✗ {probe.Error}";
            }
        }

        /// <summary>Send a minimal test prompt to the active provider.</summary>
        public async Task TestAiProviderAsync()
        {
            if (!CanTestAi) return;
            AiIsTestingProvider = true;
            AiStatus            = "Testing...";
            AiStatusColor       = "Gray";

            var svc    = new AiService(BuildAiSettings());
            var (ok, msg) = await svc.TestProviderAsync();

            AiStatus      = msg;
            AiStatusColor = ok ? "Green" : "Red";
            AiIsTestingProvider = false;
        }

        // ── Generation settings ───────────────────────────────────────────────
        private double _aiTemperature = 0.3;
        private int    _aiMaxTokens   = 1024;

        public double AiTemperature
        {
            get => _aiTemperature;
            set { _aiTemperature = value; OnPropertyChanged(); }
        }
        public int AiMaxTokens
        {
            get => _aiMaxTokens;
            set { _aiMaxTokens = value; OnPropertyChanged(); }
        }

        /// <summary>Build a transient AiSettings from current UI values (used for test + probe).</summary>
        public AiSettings BuildAiSettings() => new()
        {
            Provider      = Enum.TryParse<Core.AI.AiProvider>(_aiProvider, out var p) ? p : Core.AI.AiProvider.None,
            ClaudeApiKey  = _claudeApiKey,
            ClaudeModel   = _claudeModel,
            OpenAiApiKey  = _openAiApiKey,
            OpenAiModel   = _openAiModel,
            OllamaBaseUrl = _ollamaBaseUrl,
            OllamaModel   = _ollamaModel,
            Temperature   = _aiTemperature,
            MaxTokens     = _aiMaxTokens
        };

        #endregion
    }
}
