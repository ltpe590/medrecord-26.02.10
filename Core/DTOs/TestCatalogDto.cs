namespace Core.DTOs
{
    public sealed class TestCatalogDto
    {
        public int    TestId      { get; init; }
        public string TestName    { get; init; } = string.Empty;

        // SI unit + range
        public string TestUnit    { get; init; } = string.Empty;   // SI unit (primary)
        public string NormalRange { get; init; } = string.Empty;   // SI normal range

        // Imperial / conventional unit + range (optional)
        public string UnitImperial        { get; init; } = string.Empty;
        public string NormalRangeImperial { get; init; } = string.Empty;

        /// <summary>Returns all non-empty unit+range options for the dropdown.</summary>
        public List<LabUnitOption> UnitOptions
        {
            get
            {
                var opts = new List<LabUnitOption>();
                if (!string.IsNullOrWhiteSpace(TestUnit))
                    opts.Add(new LabUnitOption(TestUnit, NormalRange));
                if (!string.IsNullOrWhiteSpace(UnitImperial))
                    opts.Add(new LabUnitOption(UnitImperial, NormalRangeImperial));
                return opts;
            }
        }
    }

    /// <summary>One selectable unit + its normal range for a test.</summary>
    public sealed record LabUnitOption(string Unit, string NormalRange)
    {
        public override string ToString() => string.IsNullOrWhiteSpace(NormalRange)
            ? Unit
            : $"{Unit}   (Ref: {NormalRange})";
    }
}
