namespace Core.DTOs
{
    public sealed class TestCatalogDto
    {
        public int TestId { get; init; }
        public string TestName { get; init; } = string.Empty;
        public string TestUnit { get; init; } = string.Empty;
        public string NormalRange { get; init; } = string.Empty;
    }
}
