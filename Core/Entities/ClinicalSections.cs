namespace Core.Entities;

using Core.Validators;
using System.Collections.Generic;
using System.Linq;

public static class ClinicalSections
{
    /// <summary>
    /// Generic history section - typically replaced by profile-specific history sections.
    /// Profiles with ReplacesGenericHistory=true will remove this and add their own history sections.
    /// </summary>
    public const string History = "History";

    public const string Examination = "Examination";
    public const string Assessment = "Assessment";
    public const string Laboratory = "Laboratory";
    public const string Imaging = "Imaging";
    public const string Management = "Management";
    public const string Medications = "Medications";
    public const string Measurements = "Measurements";

    /// <summary>
    /// All default clinical sections. Note: History is included for compatibility but is typically
    /// replaced by profile-specific history sections. See ISpecialtyProfile.ReplacesGenericHistory.
    /// </summary>
    public static readonly IReadOnlySet<string> All =
        new HashSet<string>
        {
            History,
            Examination,
            Assessment,
            Laboratory,
            Imaging,
            Management,
            Medications,
            Measurements
        };

    // Cached dictionary for case-insensitive lookups
    private static readonly Dictionary<string, string> SectionLookup =
        All.ToDictionary(s => s.ToLowerInvariant(), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a section by name (case-insensitive).
    /// </summary>
    /// <param name="name">The section name to look up</param>
    /// <returns>The canonical section name if found, null otherwise</returns>
    public static string? FromName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return SectionLookup.TryGetValue(name.ToLowerInvariant(), out var section) ? section : null;
    }

    /// <summary>
    /// Validates if a section exists.
    /// </summary>
    public static bool IsValid(string name) => !string.IsNullOrWhiteSpace(name) &&
        SectionLookup.ContainsKey(name.ToLowerInvariant());

    /// <summary>
    /// Gets a section by name, throws if not found.
    /// </summary>
    /// <param name="name">The section name</param>
    /// <returns>The canonical section name</returns>
    /// <exception cref="ArgumentException">Thrown if the section is not found</exception>
    public static string GetByName(string name)
    {
        StringValidator.ValidateNotEmpty(name, nameof(name));
        return FromName(name) ?? throw new ArgumentException(
            $"Unknown clinical section: {name}", nameof(name));
    }

    /// <summary>
    /// Gets all section names as an ordered list.
    /// </summary>
    public static IEnumerable<string> GetAllSections() => All.OrderBy(s => s);
}