using Core.Entities;
using System;
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

        protected VisitEntry() { } // EF only

        public VisitEntry(int visitId, string section, string content, ClinicalSystem? system = null)
        {
            SetSection(section);
            SetContent(content);
            VisitId = visitId;
            SystemCode = system?.Code;
        }

        public void Update(string section, string content, ClinicalSystem? system)
        {
            SetSection(section);
            SetContent(content);
            SystemCode = system?.Code;
        }

        private void SetSection(string section)
        {
            if (string.IsNullOrWhiteSpace(section))
                throw new ArgumentException("Visit Section is required.");

            Section = section.Trim();
        }


        private void SetContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Content is required.");

            Content = content.Trim();
        }
    }
}
