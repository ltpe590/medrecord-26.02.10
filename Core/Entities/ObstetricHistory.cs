using Core.Interfaces.Services;
using Core.Interfaces;
using Core.Profiles;

namespace Core.Entities
{
    public class ObstetricHistory
    {
        public int? Gravida { get; private set; }
        public int? Para { get; private set; }
        public int? Abortion { get; private set; }
        public DateOnly? LMP { get; private set; }

        private readonly List<VisitEntry> _entries = new();
        public IReadOnlyList<VisitEntry> Entries => _entries;

        public const string MenstrualHistory = "Menstrual History";
        public const string ObstetricalHistory = "Obstetric History";
        public const string GynecologicalHistory = "Gynecologic History";

        public static readonly IReadOnlyList<string> All =
        [
            MenstrualHistory,
            ObstetricalHistory,
            GynecologicalHistory
        ];

        public ObstetricHistory(int? gravida = null, int? para = null, int? abortion = null, DateOnly? lmp = null)
        {
            Gravida = gravida;
            Para = para;
            Abortion = abortion;
            LMP = lmp;
        }

        public void Update(int? gravida = null, int? para = null, int? abortion = null, DateOnly? lmp = null)
        {
            Gravida = gravida ?? Gravida;
            Para = para ?? Para;
            Abortion = abortion ?? Abortion;
            LMP = lmp ?? LMP;
        }
    }

    #region Extensions
    public static class ObGynVisitExtensions
    {
        public static void AddObGynHistory(this Visit visit, ObGyneProfile profile, string section, string content)
        {
            visit.AddEntry(profile, section, content, ClinicalSystem.GyneOb);
        }
    }
    #endregion
}
