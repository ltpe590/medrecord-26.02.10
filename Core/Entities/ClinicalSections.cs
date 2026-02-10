namespace Core.Entities;

public static class ClinicalSections
{
    public const string History = "History";
    public const string Examination = "Examination";
    public const string Assessment = "Assessment";
    public const string Laboratory = "Laboratory";
    public const string Imaging = "Imaging";
    public const string Management = "Management";
    public const string Medications = "Medications";
    public const string Measurements = "Measurements";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>
        {
            
            Examination,
            Assessment,
            Laboratory,
            Imaging,
            Management,
            Medications,
            Measurements
        };
}
