using Core.Entities;
using Core.Interfaces.Services;
using System.Windows;
using System.Windows.Media;

namespace WPF.Services
{
    /// <summary>
    /// Applies accent color + dark/light mode to Application.Current.Resources at runtime.
    /// Call Apply() once at startup and again whenever settings change.
    /// </summary>
    public static class ThemeService
    {
        // ── Specialty → accent hex ──────────────────────────────────────────────
        private static readonly Dictionary<string, string> SpecialtyAccents = new(StringComparer.OrdinalIgnoreCase)
        {
            { "General",                    "#2196F3" },
            { "Gynecology / Obstetrics",    "#E91E8C" },
            { "Cardiovascular",             "#F44336" },
            { "Neurological",               "#9C27B0" },
            { "Respiratory",                "#00BCD4" },
            { "Gastrointestinal",           "#FF9800" },
            { "Musculoskeletal",            "#795548" },
            { "Orthopedic",                 "#795548" },
            { "Dermatology",                "#FF5722" },
            { "Psychiatric",                "#607D8B" },
            { "Endocrine",                  "#4CAF50" },
            { "Ophthalmology",              "#03A9F4" },
            { "Hematology",                 "#B71C1C" },
            { "Renal",                      "#1565C0" },
            { "Uncategorized",              "#546E7A" },
        };

        // ── Named color schemes (independent override) ──────────────────────────
        public static readonly Dictionary<string, string> ColorSchemes = new()
        {
            { "SpecialtyLinked", "" },          // resolved at runtime from specialty
            { "Blue",   "#2196F3" },
            { "Green",  "#4CAF50" },
            { "Purple", "#9C27B0" },
            { "Rose",   "#E91E8C" },
            { "Slate",  "#546E7A" },
            { "Amber",  "#FF8F00" },
        };

        // ── Public list for ComboBox binding ────────────────────────────────────
        public static IReadOnlyList<string> SchemeNames => ColorSchemes.Keys.ToList();

        // ── Entry point ─────────────────────────────────────────────────────────
        public static void Apply(IAppSettingsService settings)
        {
            var accent = ResolveAccent(settings.ColorScheme, settings.DoctorSpecialty);
            ApplyToResources(accent, settings.IsDarkMode);
        }

        public static void Apply(string colorScheme, string specialty, bool darkMode)
        {
            var accent = ResolveAccent(colorScheme, specialty);
            ApplyToResources(accent, darkMode);
        }

        // ── Returns the hex accent for a given scheme/specialty combo ────────────
        public static string ResolveAccent(string colorScheme, string specialty)
        {
            if (colorScheme == "SpecialtyLinked" || string.IsNullOrWhiteSpace(colorScheme))
            {
                return SpecialtyAccents.TryGetValue(specialty ?? "", out var sa)
                    ? sa
                    : "#2196F3";
            }
            return ColorSchemes.TryGetValue(colorScheme, out var sc) ? sc : "#2196F3";
        }

        // ── Returns the default accent for a specialty (for preview) ─────────────
        public static string GetSpecialtyAccent(string specialty)
            => SpecialtyAccents.TryGetValue(specialty ?? "", out var c) ? c : "#2196F3";

        // ── Injects resources into Application.Current.Resources ─────────────────
        private static void ApplyToResources(string accentHex, bool darkMode)
        {
            if (Application.Current == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                var res = Application.Current.Resources;

                var accent      = ParseBrush(accentHex);
                var accentDark  = Darken(accent, 0.15);
                var accentLight = Lighten(accent, 0.85);

                // Accent colors
                res["AppAccent"]        = accent;
                res["AppAccentDark"]    = accentDark;
                res["AppAccentLight"]   = accentLight;
                res["AppAccentHex"]     = accentHex;

                // Background / surface / foreground
                if (darkMode)
                {
                    res["AppBackground"]    = new SolidColorBrush(Color.FromRgb(18,  18,  18));
                    res["AppSurface"]       = new SolidColorBrush(Color.FromRgb(30,  30,  30));
                    res["AppSurface2"]      = new SolidColorBrush(Color.FromRgb(42,  42,  42));
                    res["AppForeground"]    = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                    res["AppForeground2"]   = new SolidColorBrush(Color.FromRgb(170, 170, 170));
                    res["AppBorder"]        = new SolidColorBrush(Color.FromRgb(55,  55,  55));
                }
                else
                {
                    res["AppBackground"]    = new SolidColorBrush(Color.FromRgb(250, 250, 250));
                    res["AppSurface"]       = new SolidColorBrush(Colors.White);
                    res["AppSurface2"]      = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                    res["AppForeground"]    = new SolidColorBrush(Color.FromRgb(26,  26,  26));
                    res["AppForeground2"]   = new SolidColorBrush(Color.FromRgb(102, 102, 102));
                    res["AppBorder"]        = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                }
            });
        }

        private static SolidColorBrush ParseBrush(string hex)
        {
            try { return (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!; }
            catch { return Brushes.DodgerBlue; }
        }

        private static SolidColorBrush Darken(SolidColorBrush brush, double amount)
        {
            var c = brush.Color;
            return new SolidColorBrush(Color.FromRgb(
                (byte)Math.Max(0, c.R - 255 * amount),
                (byte)Math.Max(0, c.G - 255 * amount),
                (byte)Math.Max(0, c.B - 255 * amount)));
        }

        private static SolidColorBrush Lighten(SolidColorBrush brush, double alpha)
        {
            var c = brush.Color;
            return new SolidColorBrush(Color.FromArgb(
                (byte)(255 * (1 - alpha)),
                c.R, c.G, c.B));
        }
    }
}
