namespace Core.DTOs
{
    public enum VisitSaveType
    {
        New,
        Resume,
        Edit
    }

    public class VisitSaveRequest
    {
        public VisitSaveType SaveType { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string AuthToken { get; set; } = string.Empty;

        public string Diagnosis { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public decimal Temperature { get; set; }
        public int BloodPressureSystolic { get; set; }
        public int BloodPressureDiastolic { get; set; }
        public int Gravida { get; set; }
        public int Para { get; set; }
        public int Abortion { get; set; }
        public DateTime? LMPDate { get; set; }

        public int? VisitId { get; set; }

        public string? PausedVisitFileName { get; set; }
    }

    public class VisitSaveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? VisitId { get; set; }
        public DateTime? VisitDate { get; set; }

        // Static Factory Methods
        public static VisitSaveResult CreateSuccess(int visitId, DateTime visitDate)
        {
            return new VisitSaveResult
            {
                Success = true,
                VisitId = visitId,
                VisitDate = visitDate,
                Message = "Visit saved successfully"
            };
        }

        public static VisitSaveResult CreateFailure(string message)
        {
            return new VisitSaveResult
            {
                Success = false,
                Message = message,
                VisitId = null,
                VisitDate = null
            };
        }

        public static VisitSaveResult CreateSuccess(int visitId, DateTime visitDate, string message)
        {
            return new VisitSaveResult
            {
                Success = true,
                VisitId = visitId,
                VisitDate = visitDate,
                Message = message
            };
        }

        public static VisitSaveResult CreateFailure(string message, Exception? exception = null)
        {
            var fullMessage = exception != null
                ? $"{message}: {exception.Message}"
                : message;

            return new VisitSaveResult
            {
                Success = false,
                Message = fullMessage,
                VisitId = null,
                VisitDate = null
            };
        }
    }
}