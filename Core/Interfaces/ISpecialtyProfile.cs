using Core.Entities;

namespace Core.Interfaces;

public interface ISpecialtyProfile
{
    string Name { get; }
    ClinicalSystem System { get; }                 // optional grouping tag
    bool ReplacesGenericHistory { get; }
    IReadOnlyList<string> Sections { get; }        // UI structure in correct order
}
