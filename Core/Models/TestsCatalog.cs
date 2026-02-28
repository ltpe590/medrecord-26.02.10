using System.ComponentModel.DataAnnotations;

namespace Core.Models
{
    /// <summary>
    /// EF-mapped catalog entry for a lab test.
    /// Scalar properties are init-only: set at construction, not mutated afterward.
    /// Navigation stays settable for EF loading.
    /// </summary>
    public class TestsCatalog
    {
        [Key]
        public int TestId { get; set; }              // set: EF assigns after INSERT

        public required string TestName            { get; init; }
        public required string TestUnit            { get; init; }
        public required string NormalRange         { get; init; }
        public string?         UnitImperial        { get; init; }
        public string?         NormalRangeImperial { get; init; }

        // Navigation — settable so EF can populate during loading
        public virtual ICollection<LabResults> LabResults { get; set; } = new List<LabResults>();
    }
}
