using System.ComponentModel.DataAnnotations;

namespace Core.DTOs
{
    public sealed class TestCatalogCreateDto
    {
        [Required, StringLength(100)]
        public string TestName { get; init; } = string.Empty;

        [StringLength(50)]
        public string? TestUnit { get; init; }

        [StringLength(100)]
        public string? NormalRange { get; init; }
    }
}