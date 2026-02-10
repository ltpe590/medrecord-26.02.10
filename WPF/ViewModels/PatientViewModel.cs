using Core.DTOs;
using Core.Helpers;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPF.ViewModels
{
    public sealed class PatientViewModel
    {
        public int PatientId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Sex { get; init; }
        public DateTime DateOfBirth { get; init; }
        public string? PhoneNumber { get; init; }
        public string? Address { get; init; }

        public string DisplayName => Name;
        public int Age => AgeCalculator.FromDateOfBirth(DateOnly.FromDateTime(DateOfBirth));
    }
}