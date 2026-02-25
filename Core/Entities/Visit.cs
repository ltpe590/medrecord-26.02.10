using Core.Interfaces;
using Core.Validators;

namespace Core.Entities
{
    public class Visit
    {
        public int VisitId { get; private set; }
        public int PatientId { get; private set; }

        public DateTime StartedAt { get; private set; }
        public DateTime? EndedAt { get; private set; }

        // Pause as STATE (replaces VisitPause table)
        public DateTime? PausedAt { get; private set; }

        public bool IsPaused => PausedAt != null && EndedAt == null;

        public string PresentingSymptomText { get; private set; } = null!;
        public string PresentingSymptomDurationText { get; private set; } = null!;
        public string ShortNote { get; private set; } = null!;

        public ICollection<VisitEntry> Entries { get; private set; } = new List<VisitEntry>();
        public Vitals? Vitals { get; private set; }

        protected Visit()
        { } // EF

        public Visit(int patientId, string presentingSymptom, string duration, string shortNote)
        {
            PatientId = patientId;
            StartedAt = DateTime.UtcNow;
            UpdatePresentingSymptom(presentingSymptom, duration, shortNote);
        }

        public void UpdatePresentingSymptom(string symptom, string duration, string shortNote)
        {
            StringValidator.ValidateNotEmpty(symptom, nameof(symptom));
            // duration and shortNote are optional when starting a visit from the WPF client

            PresentingSymptomText = symptom.Trim();
            PresentingSymptomDurationText = string.IsNullOrWhiteSpace(duration) ? "N/A" : duration.Trim();
            ShortNote = string.IsNullOrWhiteSpace(shortNote) ? string.Empty : shortNote.Trim();
        }

        // Upsert section text (save only filled ones)
        public VisitEntry? AddEntry(ISpecialtyProfile profile, string section, string content, ClinicalSystem? system = null)
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            var existing = Entries.FirstOrDefault(e =>
                e.Section.Equals(section, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                existing.Update(section, content, system);
                return existing;
            }

            var entry = new VisitEntry(VisitId, section, content, system);
            Entries.Add(entry);
            return entry;
        }

        // Keep for compatibility with ProfileService, but do NOT create empty DB rows
        public void InitializeVisitEntries(IEnumerable<string> allSections)
        {
            // Intentionally empty.
            // UI shows all sections; DB stores only filled sections via AddEntry().
        }

        public void UpdateVitals(decimal? temperature, int? systolic, int? diastolic)
        {
            Vitals = new Vitals(temperature, systolic, diastolic);
        }

        public void Pause()
        {
            if (EndedAt != null)
                throw new InvalidOperationException("Cannot pause a finished visit.");

            if (IsPaused)
                return; // Already paused

            PausedAt = DateTime.UtcNow;
        }

        public void Resume()
        {
            if (!IsPaused)
                return; // Not paused, nothing to resume

            PausedAt = null;
        }

        public void EndVisit()
        {
            if (EndedAt != null)
                return; // Already ended

            PausedAt = null; // Clear pause state when ending
            EndedAt = DateTime.UtcNow;
        }
    }
}