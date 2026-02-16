using Core.DTOs;
using Core.Entities;
using System.Globalization;

namespace WPF.Mappers
{
    public interface IVisitMapper
    {
        string ToDisplayString(VisitDto visit);

        string ToDisplayString(Visit visit);
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

        public string ToDisplayString(Visit v)
        {
            var date = v.StartedAt.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
            var diagnosis = string.IsNullOrWhiteSpace(v.PresentingSymptomText) ? "No diagnosis" : v.PresentingSymptomText;
            var notes = string.IsNullOrWhiteSpace(v.ShortNote) ? string.Empty : $"\n📝 {v.ShortNote}";
            return $"📅 {date}\n🩺 {diagnosis}{notes}";
        }

        public VisitCreateDto CreateVisitDto(int patientId, string diagnosis, string notes,
            string temperature, string bpSystolic, string bpDiastolic)
        {
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
                BloodPressureDiastolic = SafeParseInt(bpDiastolic)
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