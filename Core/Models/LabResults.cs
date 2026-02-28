using Core.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models
{
    /// <summary>
    /// EF-mapped lab result recorded against a visit.
    /// Scalar properties are init-only: set at construction, not mutated afterward.
    /// Navigation properties stay settable for EF loading.
    /// </summary>
    public class LabResults
    {
        [Key]
        public int LabId { get; set; }              // set: EF assigns after INSERT

        [Required] public int    TestId      { get; init; }
        [Required] public int    VisitId     { get; init; }

        [StringLength(500)] public string ResultValue { get; init; } = string.Empty;
        [StringLength(50)]  public string Unit        { get; init; } = string.Empty;
        [StringLength(100)] public string NormalRange { get; init; } = string.Empty;
        [StringLength(500)] public string Notes       { get; init; } = string.Empty;

        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        // Navigations — settable so EF can populate during loading
        public virtual Visit? Visit { get; set; }
        [ForeignKey("TestId")]
        public virtual required TestsCatalog TestCatalog { get; set; }
    }
}
