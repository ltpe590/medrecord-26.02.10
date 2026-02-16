using Core.Validators;
using Core.ValueObjects;

namespace Core.Entities
{
    public class Patient
    {
        protected Patient()
        { }

        public Patient(string name, Sex sex, DateOnly dateOfBirth)
        {
            StringValidator.ValidateNotEmpty(name, nameof(name));
            DateValidator.ValidateDateOfBirth(dateOfBirth, nameof(dateOfBirth));

            Name = name.Trim();
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
            if (address != null)
                StringValidator.ValidateNotEmpty(address, nameof(address));

            PhoneNumber = phone;
            Address = address?.Trim();
            UpdateTimestamp();
        }

        public void UpdateClinicalInfo(string? bloodGroup, string? allergies, string? shortNote)
        {
            if (bloodGroup != null)
                StringValidator.ValidateNotEmpty(bloodGroup, nameof(bloodGroup));
            if (allergies != null)
                StringValidator.ValidateNotEmpty(allergies, nameof(allergies));
            if (shortNote != null)
                StringValidator.ValidateNotEmpty(shortNote, nameof(shortNote));

            BloodGroup = bloodGroup?.Trim();
            Allergies = allergies?.Trim();
            ShortNote = shortNote?.Trim();
            UpdateTimestamp();
        }

        public void UpdateIdentity(string name, Sex sex, DateOnly dob)
        {
            StringValidator.ValidateNotEmpty(name, nameof(name));
            DateValidator.ValidateDateOfBirth(dob, nameof(dob));

            Name = name.Trim();
            Sex = sex;
            DateOfBirth = dob;
            UpdateTimestamp();
        }

        public void SoftDelete()
        {
            IsDeleted = true;
            UpdateTimestamp();
        }

        private void UpdateTimestamp()
        {
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