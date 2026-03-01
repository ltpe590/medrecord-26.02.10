using Core.AI;
using Core.Interfaces.Services;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using WPF.Extensions;
using WPF.Helpers;
using WPF.Services;
using WPF.ViewModels;
using WPF.Views;

namespace WPF
{
    public partial class MainWindow : Window
    {
        #region Fields
        private readonly MainWindowViewModel            _viewModel;
        private readonly VoiceDictationService          _dictation;
        private readonly IAiService                     _aiService;
        private readonly Func<SettingsWindow>           _settingsWindowFactory;
        private readonly Func<RegisterPatientViewModel> _registerPatientVmFactory;
        private readonly IAppSettingsService            _appSettings;
        private readonly SettingsViewModel              _settingsVm;
        private readonly AppointmentsTabViewModel       _appointmentsVm;
        private bool                                    _appointmentsTabInitialised;
        private VisitPageViewModel? Visit => _viewModel.CurrentVisit;
        #endregion

        #region Constructor
        public MainWindow(
            MainWindowViewModel            viewModel,
            VoiceDictationService          dictation,
            IAiService                     aiService,
            Func<SettingsWindow>           settingsWindowFactory,
            Func<RegisterPatientViewModel> registerPatientVmFactory,
            IAppSettingsService            appSettings,
            SettingsViewModel              settingsVm,
            AppointmentsTabViewModel       appointmentsVm)
        {
            Debug.WriteLine("=== MainWindow Constructor START ===");
            try
            {
                InitializeComponent();
                _viewModel                = viewModel;
                _dictation                = dictation;
                _aiService                = aiService;
                _settingsWindowFactory    = settingsWindowFactory;
                _registerPatientVmFactory = registerPatientVmFactory;
                _appSettings              = appSettings;
                _settingsVm               = settingsVm;
                _appointmentsVm           = appointmentsVm;
                DataContext = _viewModel;

                PatientsTabContent.DataContext     = _viewModel;
                AppointmentsTabContent.DataContext = _appointmentsVm;
                SettingsTabContent.DataContext     = _settingsVm;

                FlowDirection = CultureHelper.ApplyCulture(_viewModel.AppLanguageTag);

                _viewModel.OnShowErrorMessage   += (t, m) => MessageBox.Show(m, t, MessageBoxButton.OK, MessageBoxImage.Error);
                _viewModel.OnShowSuccessMessage += (m)    => MessageBox.Show(m, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                _viewModel.OnShowWarningMessage += (t, m) => MessageBox.Show(m, t, MessageBoxButton.OK, MessageBoxImage.Warning);
                _viewModel.OnShowInfoMessage    += (t, m) => MessageBox.Show(m, t, MessageBoxButton.OK, MessageBoxImage.Information);

                _appointmentsVm.OnShowError   += (t, m) => MessageBox.Show(m, t, MessageBoxButton.OK, MessageBoxImage.Error);
                _appointmentsVm.OnShowSuccess += m       => MessageBox.Show(m, "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                _settingsVm.OnShowInfo      += (t, m) => MessageBox.Show(m, t, MessageBoxButton.OK, MessageBoxImage.Information);
                _settingsVm.OnShowError     += (t, m) => MessageBox.Show(m, t, MessageBoxButton.OK, MessageBoxImage.Error);
                _settingsVm.OnShowWarning   += (t, m) => MessageBox.Show(m, t, MessageBoxButton.OK, MessageBoxImage.Warning);
                _settingsVm.OnConfirmDialog += (t, m) =>
                    MessageBox.Show(m, t, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;

                SubscribeToViewModelEvents();
                this.Loaded          += (s, e) => Debug.WriteLine("=== MainWindow LOADED ===");
                this.ContentRendered += (s, e) => Debug.WriteLine("=== MainWindow CONTENT RENDERED ===");
                Debug.WriteLine("=== MainWindow Constructor COMPLETED ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[MainWindow] Constructor exception: " + ex.Message);
                throw;
            }
        }
        #endregion

        #region Initialization
        private void SubscribeToViewModelEvents()
        {
            _viewModel.LoginCompleted  += OnLoginCompleted;
            _viewModel.PatientsLoaded  += OnPatientsLoaded;
            _viewModel.PatientSelected += OnPatientSelected;
        }
        #endregion

        #region Tab Switching
        private void ShowTab(UIElement active)
        {
            if (!IsInitialized) return;
            PatientsTabContent.Visibility     = Visibility.Collapsed;
            AppointmentsTabContent.Visibility = Visibility.Collapsed;
            VisitTabContent.Visibility        = Visibility.Collapsed;
            SettingsTabContent.Visibility     = Visibility.Collapsed;
            active.Visibility                 = Visibility.Visible;
        }

        private void PatientsTab_Checked(object sender, RoutedEventArgs e)
            => ShowTab(PatientsTabContent);

        private async void AppointmentsTab_Checked(object sender, RoutedEventArgs e)
        {
            ShowTab(AppointmentsTabContent);
            if (!_appointmentsTabInitialised)
            {
                _appointmentsTabInitialised = true;
                await _appointmentsVm.InitAsync();
            }
        }

        private void VisitTab_Checked(object sender, RoutedEventArgs e)
            => ShowTab(VisitTabContent);

        private void SettingsTab_Checked(object sender, RoutedEventArgs e)
        {
            ShowTab(SettingsTabContent);
            var token = _viewModel.GetAuthToken();
            if (!string.IsNullOrEmpty(token))
                SettingsTabContent.SetAuthToken(token);
        }

        private async void PatientsTab_StartVisitRequested(object sender, PatientViewModel patient)
        {
            VisitTabButton.IsChecked = true;
            await _viewModel.SelectPatientAsync(patient, forceNewVisit: true);
        }
        #endregion

        #region Toolbar Handlers
        private bool _addingPatient;
        private async void AddNewPatientButton_Click(object sender, RoutedEventArgs e)
        {
            if (_addingPatient) return;
            _addingPatient = true;
            try   { await ShowNewPatientDialogAsync(); }
            finally { _addingPatient = false; }
        }
        #endregion

        #region Visit Tab Handlers
        private async void VisitTab_SaveVisitRequested(object sender, RoutedEventArgs e)
        {
            try   { await _viewModel.SaveVisitAsync(); }
            catch (Exception ex)
            { MessageBox.Show("Error saving visit:\n\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
        private async void VisitTab_CompleteVisitRequested(object sender, RoutedEventArgs e)
            => await _viewModel.CompleteVisitAsync();
        private async void VisitTab_PauseVisitRequested(object sender, RoutedEventArgs e)
            => await _viewModel.PauseVisitAsync();
        private void VisitTab_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            const double amount = 40;
            VisitTabContent.ScrollToVerticalOffset(
                VisitTabContent.VerticalOffset + (e.Delta > 0 ? -amount : amount));
            e.Handled = true;
        }
        #endregion

        #region ViewModel Event Handlers
        private async Task OnLoginCompleted()
        {
            await Dispatcher.InvokeAsync(() =>
                MessageBox.Show("Login successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information));
            var token = _viewModel.GetAuthToken();
            if (!string.IsNullOrEmpty(token))
                Dispatcher.Invoke(() => SettingsTabContent.SetAuthToken(token));
        }
        private async Task OnPatientsLoaded()
            => await Dispatcher.InvokeAsync(
                () => Debug.WriteLine("Loaded " + _viewModel.Patients.Count + " patients"));
        private async Task OnPatientSelected()
            => await Dispatcher.InvokeAsync(
                () => Debug.WriteLine("Patient selected: " + _viewModel.SelectedPatient?.Name));
        #endregion

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
                    MessageBox.Show("Patient '" + vm.CreatedPatient.Name + "' registered successfully!",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex) { this.ShowError(ex.Message, "Error Registering Patient"); }
        }
        #endregion

        #region Print Summary
        private void BtnPrintSummary_Click(object sender, RoutedEventArgs e)
        {
            var visit   = _viewModel.CurrentVisit;
            var patient = _viewModel.SelectedPatient;
            if (patient == null)
            {
                MessageBox.Show("No patient selected.", "Print Summary", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            string vitals = string.Join("   ", new[]
            {
                visit?.Temperature != 0 ? "Temp: " + visit?.Temperature + "°C" : null,
                visit?.BPSystolic  != 0 ? "BP: " + visit?.BPSystolic + "/" + visit?.BPDiastolic + " mmHg" : null
            }.Where(s => s != null));
            var win = new PrintVisitSummaryWindow(
                patientName  : patient.Name,
                patientAge   : patient.Age + " yrs / " + patient.SexDisplay,
                patientPhone : patient.Phone ?? string.Empty,
                diagnosis    : visit?.Diagnosis ?? string.Empty,
                notes        : visit?.Notes ?? string.Empty,
                vitals       : vitals,
                prescriptions: visit?.Prescriptions ?? new List<PrescriptionLineItem>(),
                labResults   : visit?.LabResults    ?? new List<LabResultLineItem>(),
                clinicName   : _appSettings.ClinicName,
                doctorTitle  : _appSettings.DoctorTitle + " " + _appSettings.DoctorName + "  .  " + _appSettings.DoctorSpecialty,
                clinicPhone  : _appSettings.ClinicPhone)
            { Owner = this };
            win.ShowDialog();
        }
        #endregion

        #region Compatibility stubs
        private void ToggleRightPanelButton_Click(object sender, RoutedEventArgs e) =>
            IsRightPanelVisible = !IsRightPanelVisible;
        private void CloseRightPanelButton_Click(object sender, RoutedEventArgs e) =>
            IsRightPanelVisible = false;
        private void ScrollViewer_PreviewMouseWheel(object sender,
            System.Windows.Input.MouseWheelEventArgs e) { }
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
        #endregion
    }
}
