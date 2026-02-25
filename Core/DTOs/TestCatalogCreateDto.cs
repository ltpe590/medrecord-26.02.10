using System.ComponentModel.DataAnnotations;

namespace Core.DTOs
{
    public sealed class TestCatalogCreateDto
    {
        [Required, StringLength(100)]
        public string TestName { get; init; } = string.Empty;

        // Primary / SI unit
        [StringLength(50)]
        public string? TestUnit { get; init; }

        [StringLength(100)]
        public string? NormalRange { get; init; }

        // Secondary / Imperial (conventional) unit
        [StringLength(50)]
        public string? UnitImperial { get; init; }

        [StringLength(100)]
        public string? NormalRangeImperial { get; init; }
    }
}
