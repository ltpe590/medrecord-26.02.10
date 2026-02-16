using Core.Interfaces;

namespace Core.Entities
{
    /// <summary>
    /// Aggregates canonical clinical systems, sections and registered specialty profiles
    /// into a single read-only catalog with helper lookups and section resolution.
    /// </summary>
    public sealed class ClinicalCatalog
    {
        public IReadOnlySet<ClinicalSystem> Systems { get; }
        public IReadOnlySet<string> Sections { get; }
        public IReadOnlyList<ISpecialtyProfile> Profiles { get; }

        private readonly Dictionary<string, ISpecialtyProfile> _profilesByName;
        private readonly Dictionary<string, List<ISpecialtyProfile>> _profilesBySystemCode;

        public ClinicalCatalog(IEnumerable<ISpecialtyProfile> profiles)
        {
            if (profiles is null)
                throw new ArgumentNullException(nameof(profiles));

            Systems = ClinicalSystem.All;
            Sections = ClinicalSections.All;

            // Materialize and ensure stable ordering
            Profiles = profiles
                .Where(p => p is not null)
                .Distinct()
                .ToList();

            _profilesByName = Profiles
                .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

            _profilesBySystemCode = Profiles
                .GroupBy(p => p.System.Code.ToUpperInvariant())
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        public ISpecialtyProfile? GetProfileByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return _profilesByName.TryGetValue(name, out var p) ? p : null;
        }

        public IEnumerable<ISpecialtyProfile> GetProfilesBySystem(ClinicalSystem system)
        {
            if (system is null)
                return Enumerable.Empty<ISpecialtyProfile>();

            return _profilesBySystemCode.TryGetValue(system.Code.ToUpperInvariant(), out var list)
                ? list
                : Enumerable.Empty<ISpecialtyProfile>();
        }

        /// <summary>
        /// Resolve clinical sections for the supplied profiles. This mirrors the
        /// behavior used throughout the codebase: remove the generic History section
        /// when a profile indicates it replaces generic history, and include profile
        /// specific sections.
        /// </summary>
        public IReadOnlyList<string> ResolveSectionsForProfiles(IEnumerable<ISpecialtyProfile> profiles)
        {
            if (profiles is null)
                throw new ArgumentNullException(nameof(profiles));

            var sections = new List<string>(Sections);

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
    }
}