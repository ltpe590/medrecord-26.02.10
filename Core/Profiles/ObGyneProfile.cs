using Core.Entities;
using Core.Interfaces;

namespace Core.Profiles;

public sealed class ObGyneProfile : ISpecialtyProfile
{
    public string Name => "Obstetric / Gynecology";
    public ClinicalSystem System => ClinicalSystem.GyneOb;

    // UI structure (ordered)
    public IReadOnlyList<string> Sections { get; } =
        ClinicalSections.All
            .Where(s =>
                !s.Equals(ClinicalSections.History, StringComparison.OrdinalIgnoreCase) &&
                !s.Equals(ClinicalSections.Examination, StringComparison.OrdinalIgnoreCase))
            .Concat(ObGyneHistorySections)
            .Concat(ObGyneExamSections)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public bool ReplacesGenericHistory => true;

    private static readonly IReadOnlyList<string> ObGyneHistorySections = new[]
    {
        "Menstrual History",
        "Obstetric History",
        "Gynecologic History",
    };

    private static readonly IReadOnlyList<string> ObGyneExamSections = new[]
    {
        "Obstetric Examination",
        "Gynecologic Examination",
    };
}
