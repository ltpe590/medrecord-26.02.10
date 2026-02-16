using Core.DTOs;
using FluentValidation;

namespace Core.Validators
{
    public class PatientCreateDtoValidator : AbstractValidator<PatientCreateDto>
    {
        public PatientCreateDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");

            RuleFor(x => x.PhoneNumber)
                .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters")
                .Matches(@"^\+?[0-9\s\-\(\)]+$").When(x => !string.IsNullOrEmpty(x.PhoneNumber))
                .WithMessage("Invalid phone number format");

            RuleFor(x => x)
                .Must(x => x.Age.HasValue || x.DateOfBirth.HasValue)
                .WithMessage("Either Age or Date of Birth must be provided");

            RuleFor(x => x.Age)
                .InclusiveBetween(0, 150).When(x => x.Age.HasValue)
                .WithMessage("Age must be between 0 and 150");

            RuleFor(x => x.BloodGroup)
                .MaximumLength(10).WithMessage("Blood group cannot exceed 10 characters");

            RuleFor(x => x.Allergies)
                .MaximumLength(500).WithMessage("Allergies cannot exceed 500 characters");

            RuleFor(x => x.Address)
                .MaximumLength(250).WithMessage("Address cannot exceed 250 characters");

            RuleFor(x => x.ShortNote)
                .MaximumLength(500).WithMessage("Short note cannot exceed 500 characters");
        }
    }

    public class PatientUpdateDtoValidator : AbstractValidator<PatientUpdateDto>
    {
        public PatientUpdateDtoValidator()
        {
            RuleFor(x => x.PatientId)
                .GreaterThan(0).WithMessage("Patient ID must be positive");

            RuleFor(x => x.Name)
                .MaximumLength(100).When(x => !string.IsNullOrEmpty(x.Name))
                .WithMessage("Name cannot exceed 100 characters");

            RuleFor(x => x.PhoneNumber)
                .MaximumLength(20).When(x => !string.IsNullOrEmpty(x.PhoneNumber))
                .WithMessage("Phone number cannot exceed 20 characters");

            RuleFor(x => x.BloodGroup)
                .MaximumLength(10).WithMessage("Blood group cannot exceed 10 characters");

            RuleFor(x => x.Allergies)
                .MaximumLength(500).WithMessage("Allergies cannot exceed 500 characters");

            RuleFor(x => x.Address)
                .MaximumLength(250).WithMessage("Address cannot exceed 250 characters");

            RuleFor(x => x.ShortNote)
                .MaximumLength(500).WithMessage("Short note cannot exceed 500 characters");
        }
    }
}