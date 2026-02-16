using Core.Validators;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities
{
    public class VisitEntry
    {
        public int VisitEntryId { get; private set; }
        public int VisitId { get; private set; }

        public string Section { get; private set; } = null!;
        public string? SystemCode { get; private set; } // Stored in DB

        [NotMapped]
        public ClinicalSystem? System => SystemCode is null ? null : ClinicalSystem.FromCode(SystemCode); // NOT stored in DB (computed)

        public string Content { get; private set; } = null!;

        protected VisitEntry()
        { } // EF only

        public VisitEntry(int visitId, string section, string content, ClinicalSystem? system = null)
        {
            VisitId = visitId;
            UpdateEntry(section, content, system);
        }

        public void Update(string section, string content, ClinicalSystem? system)
        {
            UpdateEntry(section, content, system);
        }

        private void UpdateEntry(string section, string content, ClinicalSystem? system)
        {
            StringValidator.ValidateNotEmpty(section, nameof(section));
            StringValidator.ValidateNotEmpty(content, nameof(content));

            Section = section.Trim();
            Content = content.Trim();
            SystemCode = system?.Code;
        }
    }
}