using Core.Entities;

namespace Core.Interfaces;

public interface ISpecialtyProfile
{
    string Name { get; }
    ClinicalSystem System { get; }                 // optional grouping tag

    /// <summary>
    /// Indicates whether this profile replaces the generic "History" section with
    /// profile-specific history sections. When true, ProfileService will remove the
    /// generic History section and include this profile's custom history sections instead.
    /// </summary>
    bool ReplacesGenericHistory { get; }

    /// <summary>
    /// Clinical sections specific to this profile, in the correct order for UI display.
    /// Should not include the generic History section if ReplacesGenericHistory is true.
    /// </summary>
    IReadOnlyList<string> Sections { get; }
}