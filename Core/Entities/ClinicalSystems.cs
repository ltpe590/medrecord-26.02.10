namespace Core.Entities
{
    public sealed class ClinicalSystem
    {
        public string Code { get; }
        public string Name { get; }

        private ClinicalSystem(string code, string name)
        {
            Code = code;
            Name = name;
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
            Uncategorized
        };

        // Lookup by code
        public static ClinicalSystem? FromCode(string code)
        {
            return All.FirstOrDefault(s => s.Code == code);
        }

        // Lookup by display name
        public static ClinicalSystem? FromName(string name)
        {
            return All.FirstOrDefault(s => s.Name == name);
        }

        // Validate if the name exists
        public static bool IsValid(string name) => All.Any(s => s.Name == name);

        public override string ToString() => Name;
    }
}
