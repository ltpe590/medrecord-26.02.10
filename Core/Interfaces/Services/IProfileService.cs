using Core.Entities;

namespace Core.Interfaces.Services;

public interface IProfileService
{
    IReadOnlyList<string> ResolveClinicalSections(
        IEnumerable<ISpecialtyProfile> profiles);

    void InitializeClinicalSections(
        Visit visit,
        IEnumerable<ISpecialtyProfile> profiles);
}