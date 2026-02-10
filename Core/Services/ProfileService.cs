using Core.Entities;
using Core.Interfaces;
using Core.Interfaces.Services;

namespace Core.Services;

public sealed class ProfileService : IProfileService
{
    public IReadOnlyList<string> ResolveClinicalSections(
        IEnumerable<ISpecialtyProfile> profiles)
    {
        var sections = new List<string>(ClinicalSections.All);

        foreach (var profile in profiles)
        {
            if (profile.ReplacesGenericHistory)
                sections.Remove(ClinicalSections.History);

            sections.AddRange(profile.Sections);
        }

        return sections
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public void InitializeClinicalSections(
        Visit visit,
        IEnumerable<ISpecialtyProfile> profiles)
    {
        if (visit.Entries.Any())
            return;

        var sections = ResolveClinicalSections(profiles);
        visit.InitializeVisitEntries(sections);
    }
}
