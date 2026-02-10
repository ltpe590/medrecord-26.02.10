using Core.DTOs;
using System.Globalization;

namespace WPF.Mappers
{
    public interface IVisitMapper
    {
        string ToDisplayString(VisitDto visit);
    }

    public class VisitMapper : IVisitMapper
    {
        public string ToDisplayString(VisitDto v)
        {
            var date = v.DateOfVisit.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
            var diagnosis = string.IsNullOrWhiteSpace(v.Diagnosis) ? "No diagnosis" : v.Diagnosis;
            var notes = string.IsNullOrWhiteSpace(v.Notes) ? string.Empty : $"\n📝 {v.Notes}";
            return $"📅 {date}\n🩺 {diagnosis}{notes}";
        }

        public VisitCreateDto CreateVisitDto(int patientId, string diagnosis, string notes,
            string temperature, string bpSystolic, string bpDiastolic,
            string gravida, string para, string abortion)
        {
            // Validation could go here too, or stay in the service
            if (patientId == 0)
                throw new ArgumentException("Patient ID is required");

            if (string.IsNullOrWhiteSpace(diagnosis))
                throw new ArgumentException("Diagnosis is required");

            return new VisitCreateDto
            {
                PatientId = patientId,
                DateOfVisit = DateTime.UtcNow,
                Diagnosis = diagnosis,
                Notes = notes,
                Temperature = SafeParseDecimal(temperature),
                BloodPressureSystolic = SafeParseInt(bpSystolic),
                BloodPressureDiastolic = SafeParseInt(bpDiastolic),
                Gravida = SafeParseInt(gravida),
                Para = SafeParseInt(para),
                Abortion = SafeParseInt(abortion)
            };
        }

        private static decimal SafeParseDecimal(string value)
        {
            return decimal.TryParse(value, out var result) ? result : 0;
        }

        private static int SafeParseInt(string value)
        {
            return int.TryParse(value, out var result) ? result : 0;
        }
    }
}
