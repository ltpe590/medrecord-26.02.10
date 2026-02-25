using System.Globalization;
using System.Windows;

namespace WPF.Helpers
{
    public static class CultureHelper
    {
        private static readonly HashSet<string> RtlLanguages = new(StringComparer.OrdinalIgnoreCase)
        {
            "ar", "he", "fa", "ur", "yi", "dv", "ps", "ckb", "sd"
        };

        /// <summary>
        /// Returns LeftToRight or RightToLeft based on the BCP-47 language tag.
        /// Falls back to the current thread culture if tag is null/empty.
        /// </summary>
        public static FlowDirection GetFlowDirection(string? languageTag = null)
        {
            var tag = string.IsNullOrWhiteSpace(languageTag)
                ? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
                : languageTag;

            // Extract the primary language subtag (e.g. "ar" from "ar-SA")
            var primary = tag.Split('-')[0];
            return RtlLanguages.Contains(primary)
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;
        }

        /// <summary>
        /// Applies the culture to the current thread and returns the resulting FlowDirection.
        /// Call once at startup (e.g. in App.xaml.cs or after settings load).
        /// </summary>
        public static FlowDirection ApplyCulture(string languageTag)
        {
            try
            {
                var culture = CultureInfo.GetCultureInfo(languageTag);
                Thread.CurrentThread.CurrentCulture   = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
            }
            catch (CultureNotFoundException)
            {
                // Unknown tag — leave thread culture unchanged
            }

            return GetFlowDirection(languageTag);
        }
    }
}
