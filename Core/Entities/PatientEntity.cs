using Core.ValueObjects;

namespace Core.Entities
{
    public class Patient
    {
        protected Patient() { }

        public Patient(string name, Sex sex, DateOnly dateOfBirth)
        {
            Name = name;
            Sex = sex;
            DateOfBirth = dateOfBirth;
            CreatedAt = DateTime.UtcNow;
        }

        public int PatientId { get; protected set; }

        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        public string Name { get; private set; } = string.Empty;
        public Sex Sex { get; private set; }
        public DateOnly DateOfBirth { get; private set; }

        public PhoneNumber? PhoneNumber { get; private set; }
        public string? BloodGroup { get; private set; }
        public string? Allergies { get; private set; }
        public string? Address { get; private set; }
        public string? ShortNote { get; private set; }

        public bool IsDeleted { get; private set; }

        public ICollection<Visit> Visits { get; protected set; } = new List<Visit>();

        public void UpdateContact(PhoneNumber? phone, string? address)
        {
            PhoneNumber = phone;
            Address = address;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateClinicalInfo(string? bloodGroup, string? allergies, string? shortNote)
        {
            BloodGroup = bloodGroup;
            Allergies = allergies;
            ShortNote = shortNote;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateIdentity(string name, Sex sex, DateOnly dob)
        {
            Name = name;
            Sex = sex;
            DateOfBirth = dob;
            UpdatedAt = DateTime.UtcNow;
        }


        public void SoftDelete()
        {
            IsDeleted = true;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public enum Sex
    {
        Unknown = 0,
        Male = 1,
        Female = 2
    }
}
