using Core.Entities;
using Core.Interfaces;
using Core.Validators;

namespace Core.Profiles;

/// <summary>
/// Specialty profile for Obstetric / Gynecology.
/// Replaces generic History and Examination sections with ObGyne-specific sections:
/// - History: Menstrual History, Obstetric History, Gynecologic History
/// - Examination: Obstetric Examination, Gynecologic Examination
/// </summary>
public sealed class ObGyneProfile : ISpecialtyProfile
{
    public const string ProfileName = "Obstetric / Gynecology";

    /// <summary>Custom history sections for ObGyne specialty.</summary>
    private static readonly IReadOnlyList<string> ObGyneHistorySections = new[]
    {
        "Menstrual History",
        "Obstetric History",
        "Gynecologic History",
    };

    /// <summary>Custom examination sections for ObGyne specialty.</summary>
    private static readonly IReadOnlyList<string> ObGyneExamSections = new[]
    {
        "Obstetric Examination",
        "Gynecologic Examination",
    };

    /// <summary>Special clinical fields specific to ObGyne.</summary>
    private static readonly IReadOnlyList<string> ObGyneSpecialFields = new[]
    {
        "Pregnancy Status",
        "Trimester/Weeks",
        "Parity/Gravidity",
        "Last Menstrual Period",
        "Estimated Due Date",
    };

    static ObGyneProfile()
    {
        // Validate that all custom sections have valid names
        var allSections = ObGyneHistorySections
            .Concat(ObGyneExamSections)
            .Concat(ObGyneSpecialFields);

        foreach (var section in allSections)
        {
            StringValidator.ValidateNotEmpty(section, nameof(section));
        }
    }

    public string Name => ProfileName;
    public ClinicalSystem System => ClinicalSystem.GyneOb;

    /// <summary>
    /// Clinical sections for this profile, excluding generic History and Examination
    /// which are replaced by profile-specific sections defined above.
    /// </summary>
    public IReadOnlyList<string> Sections { get; } =
        ClinicalSections.All
            .Where(s => !IsGenericHistoryOrExamination(s))
            .Concat(ObGyneHistorySections)
            .Concat(ObGyneExamSections)
            .Concat(ObGyneSpecialFields)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public bool ReplacesGenericHistory => true;

    /// <summary>
    /// Gets the custom history sections for this profile.
    /// </summary>
    public IReadOnlyList<string> HistorySections => ObGyneHistorySections;

    /// <summary>
    /// Gets the custom examination sections for this profile.
    /// </summary>
    public IReadOnlyList<string> ExaminationSections => ObGyneExamSections;

    /// <summary>
    /// Gets the special ObGyne-specific clinical fields.
    /// </summary>
    public IReadOnlyList<string> SpecialFields => ObGyneSpecialFields;

    private static bool IsGenericHistoryOrExamination(string section)
    {
        return section.Equals(ClinicalSections.History, StringComparison.OrdinalIgnoreCase) ||
               section.Equals(ClinicalSections.Examination, StringComparison.OrdinalIgnoreCase);
    }
}