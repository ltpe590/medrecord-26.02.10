using System.ComponentModel.DataAnnotations;

namespace Core.Models
{
    public class TestsCatalog
    {
        [Key]
        public int TestId { get; set; }

        public required string TestName        { get; set; } // e.g., "Glucose Fasting"

        // Primary / SI unit
        public required string TestUnit        { get; set; } // e.g., "mmol/L"
        public required string NormalRange     { get; set; } // e.g., "3.9-6.1"

        // Secondary / Imperial (conventional) unit — optional
        public string? UnitImperial            { get; set; } // e.g., "mg/dL"
        public string? NormalRangeImperial     { get; set; } // e.g., "70-110"

        public virtual ICollection<LabResults> LabResults { get; set; } = new List<LabResults>();
    }
}
