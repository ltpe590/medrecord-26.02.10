using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WPF.Converters;
using WPF.Extensions;
using WPF.ViewModels;
using WPF.Views;

namespace WPF
{
    public partial class MainWindow : Window
    {
        #region Fields

        private readonly MainWindowViewModel _viewModel;
        #endregion

        #region Constructor

        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            DataContext = _viewModel;

            _viewModel.OnShowErrorMessage += (title, message) =>
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

            _viewModel.OnShowSuccessMessage += (message) =>
                MessageBox.Show(message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);

            SubscribeToViewModelEvents();
            SetupDefaults();
        }

        #endregion

        #region Initialization

        private void SubscribeToViewModelEvents()
        {
            _viewModel.SaveVisitRequested += OnSaveVisitRequested;
            _viewModel.LoginCompleted += OnLoginCompleted;
            _viewModel.PatientsLoaded += OnPatientsLoaded;
            _viewModel.PatientSelected += OnPatientSelected;
            _viewModel.OnShowErrorMessage += ShowErrorMessage;
            _viewModel.OnShowSuccessMessage += ShowSuccessMessage;
        }

        private void SetupDefaults()
        {
            LoginExpander.IsExpanded = true;
            PatientManagementExpander.IsExpanded = false;
            VisitManagementExpander.IsExpanded = false;
        }

        #endregion

        #region Event Handlers

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _viewModel.LoginAsync(
                    TxtUsername.Text,
                    TxtPassword.Password);
            }
            catch
            {
                // Errors are already handled in ViewModel
            }
        }

        private async void RefreshPatientsButton_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.LoadAllPatientsAsync();
        }
        private void TxtPatientSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _viewModel.PatientSearchText = TxtPatientSearch.Text;
        }

        private async void PatientListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PatientListBox.SelectedItem is PatientViewModel patient)
            {
                await _viewModel.SelectPatientAsync(patient);
            }
            else
            {
                await _viewModel.SelectPatientAsync(null);
            }
        }

        private async void PatientListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not MainWindowViewModel vm)
            {
                Debug.WriteLine("❌ DoubleClick: DataContext is not MainWindowViewModel");
                return;
            }

            if (vm.SelectedPatient == null)
            {
                Debug.WriteLine("❌ DoubleClick: SelectedPatient is NULL");
                return;
            }

            Debug.WriteLine($"✅ DoubleClick: Patient {vm.SelectedPatient.PatientId} selected");

            await vm.SelectPatientAsync(vm.SelectedPatient);
        }


        private async void SaveVisitButton_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.SaveVisitAsync();
        }

        private async void AddNewPatientButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowNewPatientDialogAsync();
        }

        #endregion

        #region ViewModel Event Handlers

        private async Task OnSaveVisitRequested()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                SaveVisitButton.Background = Brushes.LightGreen;

                Task.Delay(1000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        SaveVisitButton.Background = Brushes.LightGray;
                    });
                });
            });
        }

        private async Task OnLoginCompleted()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                StatusText.Text = "Login successful";
                MessageBox.Show("Login successful!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        private async Task OnPatientsLoaded()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                StatusText.Text = $"Loaded {_viewModel.Patients.Count} patients";
            });
        }

        private async Task OnPatientSelected()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                if (_viewModel.SelectedPatient != null)
                {
                    SelectedPatientInfo.Text = _viewModel.SelectedPatientInfo;
                    SelectedPatientDetails.Text = _viewModel.SelectedPatientDetails;
                    HistoryTextBlock.Text = _viewModel.PatientHistory;
                }
            });
        }

        private void ShowErrorMessage(string message, string title)
        {
            Dispatcher.Invoke(() =>
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error));
        }

        private void ShowSuccessMessage(string message)
        {
            Dispatcher.Invoke(() =>
                MessageBox.Show(message, "Success", MessageBoxButton.OK, MessageBoxImage.Information));
        }
        #endregion

        #region UI Methods

        private async Task ShowNewPatientDialogAsync()
        {
            try
            {

                var vm = App.Services.GetRequiredService<RegisterPatientViewModel>();
                var window = new RegisterPatientWindow(vm)
                {
                    Owner = this
                };

                if (window.ShowDialog() == true && vm.CreatedPatient != null)
                {
                    await _viewModel.AddNewPatientAsync(vm.CreatedPatient);

                    if (vm.StartVisitImmediately && vm.CreatedPatient != null)
                    {
                        await _viewModel.LoadAllPatientsAsync();
                        var newPatient = _viewModel.Patients
                            .OrderByDescending(p => p.PatientId)
                            .FirstOrDefault(p =>
                                p.Name.Equals(vm.CreatedPatient.Name, StringComparison.OrdinalIgnoreCase));

                        if (newPatient != null)
                            StartVisitForPatient(newPatient);
                    }
                }
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message, "Error Opening Dialog");
            }
        }

        private void StartVisitForPatient(PatientViewModel patient)
        {
            VisitManagementExpander.IsExpanded = true;
            StatusText.Text = $"Started visit for {patient.Name}";
        }

        private void DebugButton_Click(object sender, RoutedEventArgs e)
        {
            var debugWindow = App.Services.GetRequiredService<DebugWindow>();
            debugWindow.Owner = this;
            debugWindow.Show();
        }

        private async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.OpenSettingsAsync();
        }

        private async void AddLabResultButton_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.AddLabResultAsync();
        }

        private void PrintSummaryButton_Click(object sender, RoutedEventArgs e)
        {
            var patient = PatientListBox.SelectedItem as PatientViewModel;
            _viewModel.PrintVisitSummary(patient);
        }

        private void ToggleRightPanelButton_Click(object sender, RoutedEventArgs e)
        {
            IsRightPanelVisible = !IsRightPanelVisible;
        }

        private void CloseRightPanelButton_Click(object sender, RoutedEventArgs e)
        {
            IsRightPanelVisible = false;
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Optional scroll tweak
        }
        private void ClearVisitForm()
        {
            TxtDiagnosis.Clear();
            TxtNotes.Clear();
            TxtTemperature.Clear();
            TxtBPS.Clear();
            TxtBPD.Clear();
            TxtGravida.Clear();
            TxtPara.Clear();
            TxtAbortion.Clear();
            TxtPrescriptions.Clear();
            TxtResultValue.Clear();
        }

        #endregion

        #region Dependency Properties

        public bool IsRightPanelVisible
        {
            get => (bool)GetValue(IsRightPanelVisibleProperty);
            set => SetValue(IsRightPanelVisibleProperty, value);
        }

        public static readonly DependencyProperty IsRightPanelVisibleProperty =
            DependencyProperty.Register(nameof(IsRightPanelVisible), typeof(bool),
                typeof(MainWindow), new PropertyMetadata(false));

        #endregion

        #region Helper Methods


        private void UpdatePanel(Border panel, bool visible)
        {
            if (panel != null)
                panel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion
    }

}
