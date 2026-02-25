using Core.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models
{
    public class Prescription
    {
        public int PrescriptionId { get; set; }
        public int DrugId { get; set; }
        public string? Dosage { get; set; }
        public string? DurationDays { get; set; }
        public string? Route { get; set; }
        public string? Frequency { get; set; }
        public string? Instructions { get; set; }

        public int VisitId { get; set; }

        public required Visit Visit { get; set; }

        [ForeignKey("DrugId")]
        public required DrugCatalog DrugCatalog { get; set; }
    }
}