namespace Core.DTOs
{
    public class LabResultsDto
    {
        public int    LabId       { get; set; }
        public int    TestId      { get; set; }
        public int    VisitId     { get; set; }
        public string ResultValue { get; set; } = string.Empty;
        public string Unit        { get; set; } = string.Empty;
        public string NormalRange { get; set; } = string.Empty;
        public string Notes       { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // Joined from TestsCatalog
        public string TestName    { get; set; } = string.Empty;
    }
}
