namespace Core.Entities
{
    using Core.Validators;

    public sealed class ClinicalSystem
    {
        public string Code { get; }
        public string Name { get; }

        private ClinicalSystem(string code, string name)
        {
            StringValidator.ValidateNotEmpty(code, nameof(code));
            StringValidator.ValidateNotEmpty(name, nameof(name));

            Code = code.Trim();
            Name = name.Trim();
        }

        // Predefined systems
        public static readonly ClinicalSystem General = new("GEN", "General");

        public static readonly ClinicalSystem Cardiovascular = new("CVS", "Cardiovascular");
        public static readonly ClinicalSystem Respiratory = new("RESP", "Respiratory");
        public static readonly ClinicalSystem GyneOb = new("OBGYN", "Gynecology / Obstetrics");
        public static readonly ClinicalSystem Neurological = new("NEURO", "Neurological");
        public static readonly ClinicalSystem Endocrine = new("ENDO", "Endocrine");
        public static readonly ClinicalSystem Hematology = new("HEMA", "Hematology");
        public static readonly ClinicalSystem Gastrointestinal = new("GIT", "Gastrointestinal");
        public static readonly ClinicalSystem Musculoskeletal = new("MSK", "Musculoskeletal");
        public static readonly ClinicalSystem Renal = new("RENAL", "Renal");
        public static readonly ClinicalSystem Dermatology = new("DERM", "Dermatology");
        public static readonly ClinicalSystem Psychiatric = new("PSY", "Psychiatric");
        public static readonly ClinicalSystem Ophthalmology = new("OPHT", "Ophthalmology");
        public static readonly ClinicalSystem Orthopedic = new("ORTHO", "Orthopedic");
        public static readonly ClinicalSystem Uncategorized = new("UNCAT", "Uncategorized");

        // All predefined clinical systems
        public static readonly IReadOnlySet<ClinicalSystem> All = new HashSet<ClinicalSystem>
        {
            General,
            Cardiovascular,
            Respiratory,
            GyneOb,
            Neurological,
            Endocrine,
            Hematology,
            Gastrointestinal,
            Musculoskeletal,
            Renal,
            Dermatology,
            Psychiatric,
            Ophthalmology,
            Orthopedic,
            Uncategorized
        };

        // Cached dictionary for efficient lookups by code (case-insensitive)
        private static readonly Dictionary<string, ClinicalSystem> CodeLookup =
            All.ToDictionary(s => s.Code.ToUpperInvariant(), StringComparer.OrdinalIgnoreCase);

        // Cached dictionary for efficient lookups by name (case-insensitive)
        private static readonly Dictionary<string, ClinicalSystem> NameLookup =
            All.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);

        // Lookup by code (case-insensitive)
        public static ClinicalSystem? FromCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            return CodeLookup.TryGetValue(code.ToUpperInvariant(), out var system) ? system : null;
        }

        // Lookup by display name
        public static ClinicalSystem? FromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return NameLookup.TryGetValue(name, out var system) ? system : null;
        }

        // Validate if the system exists by code
        public static bool IsValidCode(string code) => !string.IsNullOrWhiteSpace(code) &&
            CodeLookup.ContainsKey(code.ToUpperInvariant());

        // Validate if the system exists by name
        public static bool IsValidName(string name) => !string.IsNullOrWhiteSpace(name) &&
            NameLookup.ContainsKey(name);

        // Get system by code, throw if not found
        public static ClinicalSystem GetByCode(string code)
        {
            StringValidator.ValidateNotEmpty(code, nameof(code));
            return FromCode(code) ?? throw new ArgumentException(
                $"Unknown clinical system code: {code}", nameof(code));
        }

        // Get system by name, throw if not found
        public static ClinicalSystem GetByName(string name)
        {
            StringValidator.ValidateNotEmpty(name, nameof(name));
            return FromName(name) ?? throw new ArgumentException(
                $"Unknown clinical system: {name}", nameof(name));
        }

        public override string ToString() => Name;

        public override bool Equals(object? obj) => obj is ClinicalSystem sys && sys.Code == Code;

        public override int GetHashCode() => Code.GetHashCode();
    }
}