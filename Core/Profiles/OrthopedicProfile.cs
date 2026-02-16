using Core.Entities;
using Core.Interfaces;
using Core.Validators;

namespace Core.Profiles;

/// <summary>
/// Specialty profile for Orthopedic Surgery.
/// Replaces generic History and Examination sections with Orthopedic-specific sections:
/// - History: Trauma History, Pain History, Joint History, Surgery History, Functional Limitation History
/// - Examination: Range of Motion Assessment, Strength Testing, Ligament Stability Testing, Joint Palpation, Neurological Assessment, Vascular Assessment
/// </summary>
public sealed class OrthopedicProfile : ISpecialtyProfile
{
    public const string ProfileName = "Orthopedic Surgery";

    /// <summary>Custom history sections for Orthopedic specialty.</summary>
    private static readonly IReadOnlyList<string> OrthopedicHistorySections = new[]
    {
        "Trauma History",
        "Pain History",
        "Joint History",
        "Surgery History",
        "Functional Limitation History",
    };

    /// <summary>Custom examination sections for Orthopedic specialty.</summary>
    private static readonly IReadOnlyList<string> OrthopedicExamSections = new[]
    {
        "Range of Motion Assessment",
        "Strength Testing",
        "Ligament Stability Testing",
        "Joint Palpation",
        "Neurological Assessment",
        "Vascular Assessment",
        "Special Tests Results",
    };

    /// <summary>Special clinical fields specific to orthopedics.</summary>
    private static readonly IReadOnlyList<string> OrthopedicSpecialFields = new[]
    {
        "Affected Joint/Area",
        "Swelling Assessment",
        "Warmth/Erythema",
        "Deformity Notes",
        "Gait Assessment",
        "Pain Severity Scale",
        "Imaging Notes",
    };

    static OrthopedicProfile()
    {
        // Validate that all custom sections have valid names
        var allSections = OrthopedicHistorySections
            .Concat(OrthopedicExamSections)
            .Concat(OrthopedicSpecialFields);

        foreach (var section in allSections)
        {
            StringValidator.ValidateNotEmpty(section, nameof(section));
        }
    }

    public string Name => ProfileName;
    public ClinicalSystem System => ClinicalSystem.Orthopedic;

    /// <summary>
    /// Clinical sections for this profile, excluding generic History and Examination
    /// which are replaced by profile-specific sections defined above.
    /// </summary>
    public IReadOnlyList<string> Sections { get; } =
        ClinicalSections.All
            .Where(s => !IsGenericHistoryOrExamination(s))
            .Concat(OrthopedicHistorySections)
            .Concat(OrthopedicExamSections)
            .Concat(OrthopedicSpecialFields)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public bool ReplacesGenericHistory => true;

    /// <summary>
    /// Gets the custom history sections for this profile.
    /// </summary>
    public IReadOnlyList<string> HistorySections => OrthopedicHistorySections;

    /// <summary>
    /// Gets the custom examination sections for this profile.
    /// </summary>
    public IReadOnlyList<string> ExaminationSections => OrthopedicExamSections;

    /// <summary>
    /// Gets the special orthopedic-specific clinical fields.
    /// </summary>
    public IReadOnlyList<string> SpecialFields => OrthopedicSpecialFields;

    private static bool IsGenericHistoryOrExamination(string section)
    {
        return section.Equals(ClinicalSections.History, StringComparison.OrdinalIgnoreCase) ||
               section.Equals(ClinicalSections.Examination, StringComparison.OrdinalIgnoreCase);
    }
}