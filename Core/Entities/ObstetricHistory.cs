using Core.Profiles;
using Core.Validators;

namespace Core.Entities
{
    /// <summary>
    /// Represents obstetric and gynecologic history for a patient.
    /// Stores key reproductive metrics: Gravida (total pregnancies), Para (live births),
    /// Abortion (miscarriages/abortions), and Last Menstrual Period.
    /// </summary>
    public class ObstetricHistory
    {
        // Obstetric metrics (non-negative integers)
        public int? Gravida { get; private set; }

        public int? Para { get; private set; }
        public int? Abortion { get; private set; }
        public DateOnly? LMP { get; private set; }

        private readonly List<VisitEntry> _entries = new();
        public IReadOnlyList<VisitEntry> Entries => _entries;

        /// <summary>Menstrual history section identifier.</summary>
        public const string MenstrualHistory = "Menstrual History";

        /// <summary>Obstetric history section identifier.</summary>
        public const string ObstetricalHistory = "Obstetric History";

        /// <summary>Gynecologic history section identifier.</summary>
        public const string GynecologicalHistory = "Gynecologic History";

        /// <summary>All obstetric/gynecologic history sections.</summary>
        public static readonly IReadOnlyList<string> All =
        [
            MenstrualHistory,
            ObstetricalHistory,
            GynecologicalHistory
        ];

        /// <summary>
        /// Initializes a new instance of ObstetricHistory.
        /// </summary>
        /// <param name="gravida">Total number of pregnancies (non-negative)</param>
        /// <param name="para">Number of live births (non-negative)</param>
        /// <param name="abortion">Number of miscarriages/abortions (non-negative)</param>
        /// <param name="lmp">Last menstrual period date</param>
        public ObstetricHistory(int? gravida = null, int? para = null, int? abortion = null, DateOnly? lmp = null)
        {
            ObstetricHistoryValidator.ValidateObstetricMetrics(gravida, para, abortion, lmp);

            Gravida = gravida;
            Para = para;
            Abortion = abortion;
            LMP = lmp;
        }

        /// <summary>
        /// Updates obstetric history. Null values are ignored (not cleared).
        /// </summary>
        public void Update(int? gravida = null, int? para = null, int? abortion = null, DateOnly? lmp = null)
        {
            var newGravida = gravida ?? Gravida;
            var newPara = para ?? Para;
            var newAbortion = abortion ?? Abortion;
            var newLmp = lmp ?? LMP;

            ObstetricHistoryValidator.ValidateObstetricMetrics(newGravida, newPara, newAbortion, newLmp);

            Gravida = newGravida;
            Para = newPara;
            Abortion = newAbortion;
            LMP = newLmp;
        }
    }

    #region Extensions

    /// <summary>
    /// Extension methods for Visit to support ObGyne-specific history entries.
    /// </summary>
    public static class ObGynVisitExtensions
    {
        /// <summary>
        /// Adds a gynecology/obstetric history entry to the visit.
        /// </summary>
        /// <param name="visit">The visit to add the entry to</param>
        /// <param name="profile">The ObGyne profile</param>
        /// <param name="section">The history section (from ObstetricHistory.All)</param>
        /// <param name="content">The history content</param>
        public static void AddObGynHistory(this Visit visit, ObGyneProfile profile, string section, string content)
        {
            if (!ObstetricHistory.All.Contains(section, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException($"Invalid ObGyn history section: {section}", nameof(section));

            visit.AddEntry(profile, section, content, ClinicalSystem.GyneOb);
        }
    }

    #endregion Extensions
}