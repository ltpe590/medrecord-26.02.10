using Core.DTOs;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPF.ViewModels
{
    public sealed class RegisterPatientViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        #region Fields

        private readonly ILogger<RegisterPatientViewModel> _logger;
        private string _name = string.Empty;
        private DateTime? _dateOfBirth;
        private string? _sex;
        private string? _phoneNumber;
        private string? _address;
        private string? _bloodGroup;
        private string? _allergies;
        private bool _startVisitImmediately;

        #endregion

        #region Properties

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime? DateOfBirth
        {
            get => _dateOfBirth;
            set
            {
                if (_dateOfBirth != value)
                {
                    _dateOfBirth = value;
                    OnPropertyChanged();
                }
            }
        }

        public string? Sex
        {
            get => _sex;
            set
            {
                if (_sex != value)
                {
                    _sex = value;
                    OnPropertyChanged();
                }
            }
        }

        public string? PhoneNumber
        {
            get => _phoneNumber;
            set
            {
                if (_phoneNumber != value)
                {
                    _phoneNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        public string? Address
        {
            get => _address;
            set
            {
                if (_address != value)
                {
                    _address = value;
                    OnPropertyChanged();
                }
            }
        }

        public string? BloodGroup
        {
            get => _bloodGroup;
            set
            {
                if (_bloodGroup != value)
                {
                    _bloodGroup = value;
                    OnPropertyChanged();
                }
            }
        }

        public string? Allergies
        {
            get => _allergies;
            set
            {
                if (_allergies != value)
                {
                    _allergies = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool StartVisitImmediately
        {
            get => _startVisitImmediately;
            set
            {
                if (_startVisitImmediately != value)
                {
                    _startVisitImmediately = value;
                    OnPropertyChanged();
                }
            }
        }

        public PatientCreateDto? CreatedPatient { get; private set; }

        #endregion

        #region Constructor

        public RegisterPatientViewModel(ILogger<RegisterPatientViewModel> logger)
        {
            _logger = logger;
            _logger.LogInformation("RegisterPatientViewModel initialized");
        }

        #endregion

        #region Methods

        public void SavePatient(bool startVisitImmediately = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Name) || DateOfBirth is null)
                {
                    _logger.LogWarning("SavePatient called with invalid data");
                    return;
                }

                CreatedPatient = new PatientCreateDto
                {
                    Name = Name.Trim(),
                    DateOfBirth = DateOnly.FromDateTime(DateOfBirth.Value),
                    Sex = Sex,
                    PhoneNumber = PhoneNumber,
                    Address = Address,
                    BloodGroup = BloodGroup,
                    Allergies = Allergies
                };

                StartVisitImmediately = startVisitImmediately;

                _logger.LogInformation("Patient data prepared - Name: {Name}, DOB: {DateOfBirth}",
                    CreatedPatient.Name, CreatedPatient.DateOfBirth);

                RequestClose?.Invoke(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving patient data. Name: {Name}", Name);
                throw;
            }
        }

        public void Cancel()
        {
            try
            {
                _logger.LogDebug("Patient registration cancelled");
                CreatedPatient = null;
                StartVisitImmediately = false;
                RequestClose?.Invoke(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cancel operation");
                throw;
            }
        }

        #endregion

        #region Events

        public event Action<bool>? RequestClose;
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion

        #region IDataErrorInfo Implementation

        public string Error => string.Empty;

        public string this[string columnName]
        {
            get
            {
                return columnName switch
                {
                    nameof(Name) => string.IsNullOrWhiteSpace(Name) ? "Patient name is required." : string.Empty,
                    nameof(DateOfBirth) => DateOfBirth is null ? "Date of birth is required." : string.Empty,
                    _ => string.Empty
                };
            }
        }

        #endregion
    }
}
