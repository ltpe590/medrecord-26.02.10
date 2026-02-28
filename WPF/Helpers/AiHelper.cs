using Core.AI;
using Core.DTOs;

namespace WPF.Helpers
{
    /// <summary>
    /// Thin WPF-layer shim. OCR and AI logic lives in <see cref="Core.AI.LabOcrService"/>
    /// and <see cref="Core.AI.IAiService"/>. This class simply re-exports them for
    /// call sites that already reference WPF.Helpers.AiHelper.
    /// </summary>
    public static class AiHelper
    {
        /// <inheritdoc cref="LabOcrService.ExtractAsync"/>
        public static Task<LabOcrResult?> ExtractLabResultFromImageAsync(
            string base64Image,
            string mimeType,
            IAiService? aiService = null)
            => LabOcrService.ExtractAsync(base64Image, mimeType, aiService);

        /// <summary>
        /// Sends a text prompt to the active AI provider and returns the response text,
        /// or null if no provider is configured or the call fails.
        /// </summary>
        public static async Task<string?> CompleteAsync(
            IAiService? aiService,
            string userPrompt,
            string? systemPrompt = null)
        {
            if (aiService == null) return null;
            var result = await aiService.CompleteAsync(userPrompt, systemPrompt);
            return result.Success ? result.Text : null;
        }
    }
}