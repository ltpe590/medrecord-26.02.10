using Core.DTOs;
using Core.Interfaces.Services;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace WPF.ViewModels
{
    public sealed class AddLabResultViewModel : INotifyPropertyChanged
    {
        private readonly IUserService _userService;
        private readonly string _baseUrl;
        private readonly string _token;
        private readonly int _visitId;

        private List<TestCatalogDto> _tests = new();
        private TestCatalogDto? _selectedTest;
        private string _resultValue = string.Empty;

        public List<TestCatalogDto> Tests
        {
            get => _tests;
            private set { _tests = value; OnPropertyChanged(); }
        }

        public TestCatalogDto? SelectedTest
        {
            get => _selectedTest;
            set { _selectedTest = value; OnPropertyChanged(); }
        }

        public string ResultValue
        {
            get => _resultValue;
            set { _resultValue = value; OnPropertyChanged(); }
        }

        public AddLabResultViewModel(IUserService userService, string baseUrl, string token, int visitId)
        {
            _userService = userService;
            _baseUrl = baseUrl;
            _token = token;
            _visitId = visitId;
        }

        public async Task LoadTestsAsync()
        {
            Tests = await _userService.GetTestCatalogAsync(_baseUrl, _token);
        }

        public async Task SaveAsync()
        {
            if (SelectedTest is null || string.IsNullOrWhiteSpace(ResultValue))
                return;

            var dto = new LabResultCreateDto
            {
                TestId = SelectedTest.TestId,
                VisitId = _visitId,
                ResultValue = ResultValue
            };

            await _userService.SaveLabResultAsync(dto, _baseUrl, _token);
            ResultValue = string.Empty; // reset
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
