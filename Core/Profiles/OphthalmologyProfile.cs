using Core.Entities;
using Core.Interfaces;
using Core.Validators;

namespace Core.Profiles;

/// <summary>
/// Specialty profile for Ophthalmology.
/// Replaces generic History and Examination sections with Ophthalmology-specific sections:
/// - History: Visual History, Ocular History, Family Eye History, Contact Lens History
/// - Examination: Visual Acuity, Anterior Segment Examination, Intraocular Pressure, Fundoscopy
/// </summary>
public sealed class OphthalmologyProfile : ISpecialtyProfile
{
    public const string ProfileName = "Ophthalmology";

    /// <summary>Custom history sections for Ophthalmology specialty.</summary>
    private static readonly IReadOnlyList<string> OphthalmologyHistorySections = new[]
    {
        "Visual History",
        "Ocular History",
        "Family Eye History",
        "Contact Lens History",
    };

    /// <summary>Custom examination sections for Ophthalmology specialty.</summary>
    private static readonly IReadOnlyList<string> OphthalmologyExamSections = new[]
    {
        "Visual Acuity",
        "Anterior Segment Examination",
        "Intraocular Pressure",
        "Fundoscopy",
        "Visual Fields",
        "Retinal Assessment",
    };

    /// <summary>Special clinical fields specific to ophthalmology.</summary>
    private static readonly IReadOnlyList<string> OphthalmologySpecialFields = new[]
    {
        "Right Eye Findings",
        "Left Eye Findings",
        "Refraction",
        "Pupil Reaction",
    };

    static OphthalmologyProfile()
    {
        // Validate that all custom sections have valid names
        var allSections = OphthalmologyHistorySections
            .Concat(OphthalmologyExamSections)
            .Concat(OphthalmologySpecialFields);

        foreach (var section in allSections)
        {
            StringValidator.ValidateNotEmpty(section, nameof(section));
        }
    }

    public string Name => ProfileName;
    public ClinicalSystem System => ClinicalSystem.Ophthalmology;

    /// <summary>
    /// Clinical sections for this profile, excluding generic History and Examination
    /// which are replaced by profile-specific sections defined above.
    /// </summary>
    public IReadOnlyList<string> Sections { get; } =
        ClinicalSections.All
            .Where(s => !IsGenericHistoryOrExamination(s))
            .Concat(OphthalmologyHistorySections)
            .Concat(OphthalmologyExamSections)
            .Concat(OphthalmologySpecialFields)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public bool ReplacesGenericHistory => true;

    /// <summary>
    /// Gets the custom history sections for this profile.
    /// </summary>
    public IReadOnlyList<string> HistorySections => OphthalmologyHistorySections;

    /// <summary>
    /// Gets the custom examination sections for this profile.
    /// </summary>
    public IReadOnlyList<string> ExaminationSections => OphthalmologyExamSections;

    /// <summary>
    /// Gets the special ophthalmology-specific clinical fields.
    /// </summary>
    public IReadOnlyList<string> SpecialFields => OphthalmologySpecialFields;

    private static bool IsGenericHistoryOrExamination(string section)
    {
        return section.Equals(ClinicalSections.History, StringComparison.OrdinalIgnoreCase) ||
               section.Equals(ClinicalSections.Examination, StringComparison.OrdinalIgnoreCase);
    }
}