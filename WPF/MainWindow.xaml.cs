using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WPF.Extensions;
using WPF.ViewModels;
using WPF.Views;

namespace WPF
{
    public partial class MainWindow : Window
    {
        #region Fields

        private readonly MainWindowViewModel _viewModel;
        #endregion Fields

        #region Constructor

        public MainWindow(MainWindowViewModel viewModel)
        {
            Debug.WriteLine("=== MainWindow Constructor START ===");
            Log("=== MainWindow Constructor START ===");
            
            try
            {
                Debug.WriteLine("   Calling InitializeComponent()...");
                Log("   Calling InitializeComponent()...");
                InitializeComponent();
                Debug.WriteLine("   ✅ InitializeComponent() completed");
                Log("   ✅ InitializeComponent() completed");

                _viewModel = viewModel;
                DataContext = _viewModel;
                Debug.WriteLine("   ✅ DataContext set");
                Log("   ✅ DataContext set");

                _viewModel.OnShowErrorMessage += (title, message) =>
                    MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

                _viewModel.OnShowSuccessMessage += (message) =>
                    MessageBox.Show(message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                Debug.WriteLine("   ✅ Event handlers subscribed");
                Log("   ✅ Event handlers subscribed");

                SubscribeToViewModelEvents();
                Debug.WriteLine("   ✅ ViewModel events subscribed");
                Log("   ✅ ViewModel events subscribed");
                
                // Add Loaded event handler
                this.Loaded += (s, e) =>
                {
                    Debug.WriteLine("=== MainWindow LOADED EVENT FIRED ===");
                    Log("=== MainWindow LOADED EVENT FIRED ===");
                    Log($"   ActualWidth: {ActualWidth}, ActualHeight: {ActualHeight}");
                    Log($"   IsVisible: {IsVisible}, WindowState: {WindowState}");
                };
                
                this.ContentRendered += (s, e) =>
                {
                    Debug.WriteLine("=== MainWindow CONTENT RENDERED ===");
                    Log("=== MainWindow CONTENT RENDERED ===");
                };
                
                // SetupDefaults(); // COMMENTED OUT - Login expander no longer exists
                
                Debug.WriteLine("=== MainWindow Constructor COMPLETED ===");
                Log("=== MainWindow Constructor COMPLETED ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ EXCEPTION in MainWindow Constructor: {ex.Message}");
                Debug.WriteLine($"❌ Stack Trace: {ex.StackTrace}");
                Log($"❌ EXCEPTION in MainWindow Constructor: {ex.Message}");
                Log($"❌ Stack Trace: {ex.StackTrace}");
                throw;
            }
        }
        
        private static void Log(string message)
        {
            var logFile = System.IO.Path.Combine(AppContext.BaseDirectory, "logs", "mainwindow.log");
            try
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logFile)!);
                System.IO.File.AppendAllText(logFile, $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}");
            }
            catch { }
        }

        #endregion Constructor

        #region Initialization

        private void SubscribeToViewModelEvents()
        {
            // COMMENTED OUT - Old UI elements removed during tab layout redesign
            // _viewModel.SaveVisitRequested += OnSaveVisitRequested;
            _viewModel.LoginCompleted += OnLoginCompleted;
            _viewModel.PatientsLoaded += OnPatientsLoaded;
            _viewModel.PatientSelected += OnPatientSelected;
            _viewModel.OnShowErrorMessage += ShowErrorMessage;
            _viewModel.OnShowSuccessMessage += ShowSuccessMessage;
        }

        // SetupDefaults() removed - LoginExpander no longer exists
        
        #endregion Initialization

        #region Tab Switching

        private void PatientsTab_Checked(object sender, RoutedEventArgs e)
        {
            // Show Patients content, hide Visit content
            if (PatientsTabContent != null && VisitTabContent != null)
            {
                PatientsTabContent.Visibility = Visibility.Visible;
                VisitTabContent.Visibility = Visibility.Collapsed;
                Debug.WriteLine("✅ Switched to Patients tab");
            }
        }

        private void VisitTab_Checked(object sender, RoutedEventArgs e)
        {
            // Show Visit content, hide Patients content
            if (PatientsTabContent != null && VisitTabContent != null)
            {
                PatientsTabContent.Visibility = Visibility.Collapsed;
                VisitTabContent.Visibility = Visibility.Visible;
                Debug.WriteLine("✅ Switched to Visit tab");
            }
        }

        #endregion Tab Switching

        #region Event Handlers

        // LoginButton_Click removed - login now happens in separate LoginWindow
        // private async void LoginButton_Click(object sender, RoutedEventArgs e) { ... }

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
            try
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

                Debug.WriteLine($"✅ DoubleClick: Starting visit for {vm.SelectedPatient.Name}");

                // Switch to Visit tab
                VisitTabButton.IsChecked = true;
                
                // SelectPatientAsync will start visit automatically
                await vm.SelectPatientAsync(vm.SelectedPatient);

                Debug.WriteLine($"✅ DoubleClick: Visit started successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ DoubleClick EXCEPTION: {ex.GetType().Name}");
                Debug.WriteLine($"❌ Message: {ex.Message}");
                Debug.WriteLine($"❌ Stack Trace: {ex.StackTrace}");
                MessageBox.Show($"Error starting visit:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SaveVisitButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("=== SAVE VISIT BUTTON CLICKED ===");
            try
            {
                await _viewModel.SaveVisitAsync();
                Debug.WriteLine("✅ SaveVisitAsync completed successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ SaveVisitAsync EXCEPTION: {ex.Message}");
                MessageBox.Show($"Error saving visit:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // TODO: Add these methods to MainWindowViewModel
        private void CompleteVisitButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Complete Visit - Feature coming soon!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PauseVisitButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Pause Visit - Feature coming soon!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // END COMMENTED OUT SECTION

        private async void AddNewPatientButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowNewPatientDialogAsync();
        }

        #endregion Event Handlers

        #region ViewModel Event Handlers

        // COMMENTED OUT - Old UI elements removed during tab layout redesign
        /*
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
        */

        private async Task OnLoginCompleted()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                // StatusText.Text = "Login successful"; // Removed - StatusText no longer exists
                MessageBox.Show("Login successful!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        private async Task OnPatientsLoaded()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                // StatusText.Text = $"Loaded {_viewModel.Patients.Count} patients"; // Removed - StatusText no longer exists
                Debug.WriteLine($"✅ Loaded {_viewModel.Patients.Count} patients");
            });
        }

        private async Task OnPatientSelected()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                // Patient details will automatically update via binding
                Debug.WriteLine($"✅ Patient selected: {_viewModel.SelectedPatient?.Name}");
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
        #endregion ViewModel Event Handlers

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
                        // Wait a moment for database connection to fully close
                        await Task.Delay(100);
                        
                        // AddNewPatientAsync already calls LoadAllPatientsAsync, so patients are already refreshed
                        var newPatient = _viewModel.Patients
                            .OrderByDescending(p => p.PatientId)
                            .FirstOrDefault(p =>
                                p.Name.Equals(vm.CreatedPatient.Name, StringComparison.OrdinalIgnoreCase));

                        if (newPatient != null)
                        {
                            Debug.WriteLine($"✅ Created new patient: {newPatient.Name}, starting visit...");
                            
                            // Wait another moment before starting visit
                            await Task.Delay(100);
                            
                            // Switch to Visit tab
                            VisitTabButton.IsChecked = true;
                            
                            // Start visit for new patient
                            await _viewModel.SelectPatientAsync(newPatient);
                        }
                        else
                        {
                            MessageBox.Show("Patient was created but could not be found in the list. Please select them manually.", 
                                "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message, "Error Opening Dialog");
            }
        }

        // COMMENTED OUT - Old UI elements removed during tab layout redesign
        /*
        private void StartVisitForPatient(PatientViewModel patient)
        {
            VisitManagementExpander.IsExpanded = true;
            Debug.WriteLine($"✅ Started visit for {patient.Name}");
        }
        */

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

        // COMMENTED OUT - Old UI elements removed during tab layout redesign
        /*
        private void PrintSummaryButton_Click(object sender, RoutedEventArgs e)
        {
            var patient = PatientListBox.SelectedItem as PatientViewModel;
            _viewModel.PrintVisitSummary(patient);
        }
        */

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
        
        // COMMENTED OUT - Old UI elements removed during tab layout redesign
        /*
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
        */

        #endregion UI Methods

        #region Dependency Properties

        public bool IsRightPanelVisible
        {
            get => (bool)GetValue(IsRightPanelVisibleProperty);
            set => SetValue(IsRightPanelVisibleProperty, value);
        }

        public static readonly DependencyProperty IsRightPanelVisibleProperty =
            DependencyProperty.Register(nameof(IsRightPanelVisible), typeof(bool),
                typeof(MainWindow), new PropertyMetadata(false));

        #endregion Dependency Properties

        #region Helper Methods

        private void UpdatePanel(Border panel, bool visible)
        {
            if (panel != null)
                panel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion Helper Methods
    }
}