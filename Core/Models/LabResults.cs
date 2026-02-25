using Core.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models
{
    public class LabResults
    {
        [Key]
        public int LabId { get; set; }

        [Required]
        public int TestId { get; set; }

        [Required]
        public int VisitId { get; set; }

        [StringLength(500)]
        public string ResultValue { get; set; } = string.Empty;

        /// <summary>Unit used for this specific result — may differ from catalog default.</summary>
        [StringLength(50)]
        public string Unit { get; set; } = string.Empty;

        /// <summary>Normal range as used by the lab/kit that performed this test — saved per-visit.</summary>
        [StringLength(100)]
        public string NormalRange { get; set; } = string.Empty;

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Visit? Visit { get; set; }

        [ForeignKey("TestId")]
        public virtual required TestsCatalog TestCatalog { get; set; }
    }
}
