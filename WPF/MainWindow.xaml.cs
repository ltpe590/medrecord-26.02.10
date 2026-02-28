using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using WPF.Extensions;
using WPF.Helpers;
using WPF.Services;
using WPF.ViewModels;
using Core.Interfaces.Services;
using WPF.Views;

namespace WPF
{
    public partial class MainWindow : Window
    {
        #region Fields

        private readonly MainWindowViewModel _viewModel;
        private readonly VoiceDictationService _dictation;
        private readonly Core.AI.IAiService _aiService;
        private readonly Func<WPF.Views.SettingsWindow> _settingsWindowFactory;
        private readonly Func<RegisterPatientViewModel> _registerPatientVmFactory;
        private readonly IAppSettingsService _appSettings;

        private VisitPageViewModel? Visit => _viewModel.CurrentVisit;

        #endregion Fields

        #region Constructor

        public MainWindow(MainWindowViewModel viewModel, VoiceDictationService dictation, Core.AI.IAiService aiService, Func<WPF.Views.SettingsWindow> settingsWindowFactory, Func<RegisterPatientViewModel> registerPatientVmFactory, IAppSettingsService appSettings)
        {
            Debug.WriteLine("=== MainWindow Constructor START ===");

            try
            {
                InitializeComponent();

                _viewModel = viewModel;
                _dictation = dictation;
                _aiService                = aiService;
                _settingsWindowFactory    = settingsWindowFactory;
                _registerPatientVmFactory = registerPatientVmFactory;
                _appSettings              = appSettings;
                DataContext = _viewModel;

                // Propagate DataContext to PatientsTabContent
                PatientsTabContent.DataContext = _viewModel;

                FlowDirection = CultureHelper.ApplyCulture(_viewModel.AppLanguageTag);

                _viewModel.OnShowErrorMessage += (title, message) =>
                    MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
                _viewModel.OnShowSuccessMessage += (message) =>
                    MessageBox.Show(message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                _viewModel.OnShowWarningMessage += (title, message) =>
                    MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
                _viewModel.OnShowInfoMessage += (title, message) =>
                    MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

                SubscribeToViewModelEvents();

                this.Loaded += (s, e) => Debug.WriteLine($"=== MainWindow LOADED (W:{ActualWidth} H:{ActualHeight}) ===");
                this.ContentRendered += (s, e) => Debug.WriteLine("=== MainWindow CONTENT RENDERED ===");

                Debug.WriteLine("=== MainWindow Constructor COMPLETED ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainWindow] Constructor exception: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }


        #endregion Constructor

        #region Initialization

        private void SubscribeToViewModelEvents()
        {
            _viewModel.LoginCompleted    += OnLoginCompleted;
            _viewModel.PatientsLoaded   += OnPatientsLoaded;
            _viewModel.PatientSelected  += OnPatientSelected;
        }

        #endregion Initialization

        #region Tab Switching

        private void PatientsTab_Checked(object sender, RoutedEventArgs e)
        {
            if (PatientsTabContent != null && VisitTabContent != null)
            {
                PatientsTabContent.Visibility = Visibility.Visible;
                VisitTabContent.Visibility    = Visibility.Collapsed;
            }
        }

        private void VisitTab_Checked(object sender, RoutedEventArgs e)
        {
            if (PatientsTabContent != null && VisitTabContent != null)
            {
                PatientsTabContent.Visibility = Visibility.Collapsed;
                VisitTabContent.Visibility    = Visibility.Visible;
            }
        }

        // Raised by PatientsTabControl when user double-clicks or picks "Start Visit"
        private async void PatientsTab_StartVisitRequested(object sender, PatientViewModel patient)
        {
            VisitTabButton.IsChecked = true;
            await _viewModel.SelectPatientAsync(patient, forceNewVisit: true);
        }

        #endregion Tab Switching

        #region Toolbar Handlers

        private bool _addingPatient = false;

        private async void AddNewPatientButton_Click(object sender, RoutedEventArgs e)
        {
            if (_addingPatient) return;
            _addingPatient = true;
            try   { await ShowNewPatientDialogAsync(); }
            finally { _addingPatient = false; }
        }

        private async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settingsWindow = _settingsWindowFactory();
                settingsWindow.Owner = this;
                settingsWindow.SetAuthToken(_viewModel.GetAuthToken());

                var saved = settingsWindow.ShowDialog() == true;
                if (saved)
                    await _viewModel.OnSettingsSavedAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open Settings:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion Toolbar Handlers

        #region Visit Tab Handlers

        private async void VisitTab_SaveVisitRequested(object sender, RoutedEventArgs e)
        {
            try   { await _viewModel.SaveVisitAsync(); }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving visit:\n\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void VisitTab_CompleteVisitRequested(object sender, RoutedEventArgs e)
        {
            await _viewModel.CompleteVisitAsync();
        }

        private async void VisitTab_PauseVisitRequested(object sender, RoutedEventArgs e)
        {
            await _viewModel.PauseVisitAsync();
        }

        private void VisitTab_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            const double scrollAmount = 40;
            double delta = e.Delta > 0 ? -scrollAmount : scrollAmount;
            VisitTabContent.ScrollToVerticalOffset(VisitTabContent.VerticalOffset + delta);
            e.Handled = true;
        }

        #endregion Visit Tab Handlers

        #region ViewModel Event Handlers

        private async Task OnLoginCompleted()
        {
            await Dispatcher.InvokeAsync(() =>
                MessageBox.Show("Login successful!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information));
        }

        private async Task OnPatientsLoaded()
        {
            await Dispatcher.InvokeAsync(() =>
                Debug.WriteLine($"Loaded {_viewModel.Patients.Count} patients"));
        }

        private async Task OnPatientSelected()
        {
            await Dispatcher.InvokeAsync(() =>
                Debug.WriteLine($"Patient selected: {_viewModel.SelectedPatient?.Name}"));
        }

        #endregion ViewModel Event Handlers

        #region New Patient Dialog

        private async Task ShowNewPatientDialogAsync()
        {
            try
            {
                var vm     = _registerPatientVmFactory();
                var window = new RegisterPatientWindow(vm) { Owner = this };

                if (window.ShowDialog() != true || vm.CreatedPatient == null) return;

                await _viewModel.AddNewPatientAsync(vm.CreatedPatient);

                if (vm.StartVisitImmediately)
                {
                    var newPatient = _viewModel.Patients
                        .OrderByDescending(p => p.PatientId)
                        .FirstOrDefault(p =>
                            p.Name.Equals(vm.CreatedPatient.Name, StringComparison.OrdinalIgnoreCase));

                    if (newPatient != null)
                    {
                        VisitTabButton.IsChecked = true;
                        await _viewModel.SelectPatientAsync(newPatient, forceNewVisit: true);
                    }
                }
                else
                {
                    MessageBox.Show($"Patient '{vm.CreatedPatient.Name}' registered successfully!",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message, "Error Registering Patient");
            }
        }

        #endregion New Patient Dialog

        #region Print Summary

        private void BtnPrintSummary_Click(object sender, RoutedEventArgs e)
        {
            var visit   = _viewModel.CurrentVisit;
            var patient = _viewModel.SelectedPatient;
            if (patient == null)
            {
                MessageBox.Show("No patient selected.", "Print Summary",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var settings = _appSettings;

            string vitals = string.Join("   ",
                new[] {
                    visit?.Temperature != 0 ? $"Temp: {visit?.Temperature}°C" : null,
                    visit?.BPSystolic  != 0 ? $"BP: {visit?.BPSystolic}/{visit?.BPDiastolic} mmHg" : null
                }.Where(s => s != null));

            var win = new PrintVisitSummaryWindow(
                patientName  : patient.Name,
                patientAge   : $"{patient.Age} yrs / {patient.SexDisplay}",
                patientPhone : patient.Phone ?? string.Empty,
                diagnosis    : visit?.Diagnosis ?? string.Empty,
                notes        : visit?.Notes ?? string.Empty,
                vitals       : vitals,
                prescriptions: visit?.Prescriptions ?? new List<PrescriptionLineItem>(),
                labResults   : visit?.LabResults    ?? new List<LabResultLineItem>(),
                clinicName   : settings.ClinicName,
                doctorTitle  : $"{settings.DoctorTitle} {settings.DoctorName}  ·  {settings.DoctorSpecialty}",
                clinicPhone  : settings.ClinicPhone)
            { Owner = this };

            win.ShowDialog();
        }

        #endregion Print Summary

        #region Misc stubs (kept for compatibility)

        private void ToggleRightPanelButton_Click(object sender, RoutedEventArgs e) =>
            IsRightPanelVisible = !IsRightPanelVisible;

        private void CloseRightPanelButton_Click(object sender, RoutedEventArgs e) =>
            IsRightPanelVisible = false;

        private void ScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e) { }

        private void RefreshPatientsButton_Click(object sender, RoutedEventArgs e) =>
            _ = _viewModel.LoadAllPatientsAsync();

        public bool IsRightPanelVisible
        {
            get => (bool)GetValue(IsRightPanelVisibleProperty);
            set => SetValue(IsRightPanelVisibleProperty, value);
        }

        public static readonly DependencyProperty IsRightPanelVisibleProperty =
            DependencyProperty.Register(nameof(IsRightPanelVisible), typeof(bool),
                typeof(MainWindow), new PropertyMetadata(false));

        #endregion Misc stubs
    }
}
