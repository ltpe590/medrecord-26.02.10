using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WPF.Extensions;
using WPF.Helpers;
using WPF.Services;
using WPF.ViewModels;   // PrescriptionLineItem, MainWindowViewModel
using WPF.Views;

namespace WPF
{
    public partial class MainWindow : Window
    {
        #region Fields

        private readonly MainWindowViewModel _viewModel;
        private readonly VoiceDictationService _dictation;
        private readonly Core.AI.IAiService _aiService;
        #endregion Fields

        #region Constructor

        public MainWindow(MainWindowViewModel viewModel, VoiceDictationService dictation, Core.AI.IAiService aiService)
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
                _dictation = dictation;
                _aiService = aiService;
                DataContext = _viewModel;

                // Apply culture and flow direction from settings
                CultureHelper.ApplyCulture(_viewModel.AppLanguageTag);
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
            _viewModel.LoginCompleted += OnLoginCompleted;
            _viewModel.PatientsLoaded += OnPatientsLoaded;
            _viewModel.PatientSelected += OnPatientSelected;
            // NOTE: OnShowErrorMessage and OnShowSuccessMessage are already subscribed in constructor
            // Do NOT subscribe again here - that causes double notifications!
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
            // Single click: just load patient details, do NOT start a visit
            if (PatientListBox.SelectedItem is PatientViewModel patient)
            {
                await _viewModel.SelectPatientAsync(patient, forceNewVisit: false);
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
                
                // Force new visit even if patient already selected
                await vm.SelectPatientAsync(vm.SelectedPatient, forceNewVisit: true);

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

        private async void CompleteVisitButton_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.CompleteVisitAsync();
        }

        private async void PauseVisitButton_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.PauseVisitAsync();
        }

        private void BtnPrintSummary_Click(object sender, RoutedEventArgs e)
        {
            var vm = _viewModel;
            var patient = vm.SelectedPatient;
            if (patient == null)
            {
                MessageBox.Show("No patient selected.", "Print Summary",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var settings = App.Services.GetRequiredService<Core.Interfaces.Services.IAppSettingsService>();

            string vitals = string.Join("   ",
                new[] {
                    vm.Temperature != 0 ? $"Temp: {vm.Temperature}°C" : null,
                    vm.BPSystolic  != 0 ? $"BP: {vm.BPSystolic}/{vm.BPDiastolic} mmHg" : null
                }.Where(s => s != null));

            var win = new PrintVisitSummaryWindow(
                patientName  : patient.Name,
                patientAge   : $"{patient.Age} yrs / {patient.SexDisplay}",
                patientPhone : patient.Phone ?? string.Empty,
                diagnosis    : vm.Diagnosis,
                notes        : vm.Notes,
                vitals       : vitals,
                prescriptions: vm.Prescriptions,
                labResults   : vm.LabResults,
                clinicName   : settings.ClinicName,
                doctorTitle  : $"{settings.DoctorTitle} {settings.DoctorName}  ·  {settings.DoctorSpecialty}",
                clinicPhone  : settings.ClinicPhone)
            { Owner = this };

            win.ShowDialog();
        }

        private bool _addingPatient = false;

        private async void AddNewPatientButton_Click(object sender, RoutedEventArgs e)
        {
            if (_addingPatient) return;
            _addingPatient = true;
            try
            {
                await ShowNewPatientDialogAsync();
            }
            finally
            {
                _addingPatient = false;
            }
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
                AiSuggestionsPanel.Visibility = Visibility.Collapsed;
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

                if (window.ShowDialog() != true || vm.CreatedPatient == null)
                    return;

                await _viewModel.AddNewPatientAsync(vm.CreatedPatient);

                if (vm.StartVisitImmediately)
                {
                    // AddNewPatientAsync already refreshed the patient list
                    var newPatient = _viewModel.Patients
                        .OrderByDescending(p => p.PatientId)
                        .FirstOrDefault(p =>
                            p.Name.Equals(vm.CreatedPatient.Name, StringComparison.OrdinalIgnoreCase));

                    if (newPatient != null)
                    {
                        VisitTabButton.IsChecked = true;
                        await _viewModel.SelectPatientAsync(newPatient, forceNewVisit: true);
                        // Success shown by SelectPatientAsync/StartVisitForPatientAsync
                    }
                }
                else
                {
                    // Only show "patient added" message when NOT starting a visit
                    MessageBox.Show($"Patient '{vm.CreatedPatient.Name}' registered successfully!",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message, "Error Registering Patient");
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


        // ── Prescription handlers ─────────────────────────────────────────

        private void AddPrescriptionButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.AddPrescription();
        }

        private void RemovePrescriptionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PrescriptionLineItem entry)
                _viewModel.RemovePrescription(entry);
        }

        private void TxtRxDrug_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = TxtRxDrug.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                DrugSuggestPopup.IsOpen = false;
                return;
            }

            var matches = _viewModel.AvailableDrugs
                .Where(d => d.BrandName.Contains(text, StringComparison.OrdinalIgnoreCase))
                .Take(8)
                .ToList();

            DrugSuggestList.ItemsSource = matches;
            DrugSuggestPopup.IsOpen = matches.Count > 0;
        }

        private void TxtRxDrug_LostFocus(object sender, RoutedEventArgs e)
        {
            // Delay closing so SelectionChanged can fire first
            Dispatcher.InvokeAsync(() => DrugSuggestPopup.IsOpen = false,
                System.Windows.Threading.DispatcherPriority.Background);
        }

        private void DrugSuggestList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DrugSuggestList.SelectedItem is Core.DTOs.DrugCatalogDto selected)
            {
                // AutoFillFromDrug: last-used values first, then catalog defaults, then FormDefaults
                _viewModel.AutoFillFromDrug(selected, overwrite: true);
                DrugSuggestPopup.IsOpen = false;
                TxtRxDrug.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }

        private async void BtnAddNewDrug_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new AddNewDrugDialog { Owner = this };
            dlg.BrandName = _viewModel.RxDrugText;   // pre-fill from typed text

            if (dlg.ShowDialog() != true) return;

            await _viewModel.AddNewDrugAndPrescribeAsync(
                dlg.BrandName, dlg.Form, dlg.Strength,
                dlg.Route, dlg.Frequency, dlg.Instructions);
        }

        private void BtnPrintPrescription_Click(object sender, RoutedEventArgs e)
        {
            if (!_viewModel.Prescriptions.Any())
            {
                MessageBox.Show("No prescriptions to print. Add at least one drug first.",
                    "Nothing to Print", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var patient = _viewModel.SelectedPatient;
            var settings = App.Services.GetRequiredService<Core.Interfaces.Services.IAppSettingsService>();

            var preview = new PrintPrescriptionWindow(
                patientName : patient?.Name ?? "—",
                patientAge  : patient != null ? $"{patient.Age} yrs / {patient.SexDisplay}" : "—",
                diagnosis   : _viewModel.Diagnosis,
                prescriptions: _viewModel.Prescriptions,
                clinicName  : settings.ClinicName,
                doctorTitle : $"{settings.DoctorTitle} {settings.DoctorName}  ·  {settings.DoctorSpecialty}",
                clinicPhone : settings.ClinicPhone,
                doctorName  : $"{settings.DoctorTitle} {settings.DoctorName}")
            {
                Owner = this
            };

            preview.ShowDialog();
        }

        // ── Lab result handlers ───────────────────────────────────────────────

        private void AddLabResultButton_Click(object sender, RoutedEventArgs e)
            => _viewModel.AddLabResult();

        private void RemoveLabResultButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is LabResultLineItem item)
                _viewModel.RemoveLabResult(item);
        }

        private void TxtLabTest_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = TxtLabTest.Text;
            if (string.IsNullOrWhiteSpace(text)) { LabSuggestPopup.IsOpen = false; return; }

            var matches = _viewModel.AvailableTests
                .Where(t => t.TestName.Contains(text, StringComparison.OrdinalIgnoreCase))
                .Take(8).ToList();

            LabSuggestList.ItemsSource = matches;
            LabSuggestPopup.IsOpen = matches.Count > 0;
        }

        private void TxtLabTest_LostFocus(object sender, RoutedEventArgs e)
            => Dispatcher.InvokeAsync(() => LabSuggestPopup.IsOpen = false,
                System.Windows.Threading.DispatcherPriority.Background);

        private void LabSuggestList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LabSuggestList.SelectedItem is Core.DTOs.TestCatalogDto selected)
            {
                _viewModel.LabTestSearchText    = selected.TestName;
                _viewModel.LabSelectedTestId    = selected.TestId;
                _viewModel.LabUnitOptions       = selected.UnitOptions;
                _viewModel.LabSelectedUnitOption= selected.UnitOptions.FirstOrDefault();
                // Unit and NormalRange get set by LabSelectedUnitOption setter in VM
                // Also set LabResultUnit directly for the editable ComboBox text
                if (selected.UnitOptions.Count == 0)
                    _viewModel.LabResultUnit = selected.TestUnit;
                LabSuggestPopup.IsOpen = false;
                TxtLabTest.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }

        private void CbLabUnit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CbLabUnit.SelectedItem is Core.DTOs.LabUnitOption opt)
                _viewModel.LabSelectedUnitOption = opt;
        }

        /// <summary>
        /// When the doctor picks a normal range from the dropdown, find the unit option
        /// that belongs to that range and sync the Unit ComboBox to match.
        /// The range text is formatted as "range  (unit)" — parse the unit back out.
        /// If the doctor typed a custom range (not in the list) nothing extra happens;
        /// LabNormalRange is already bound two-way so the typed value is captured.
        /// </summary>
        private void CbLabNormalRange_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CbLabNormalRange.SelectedItem is not string selected) return;
            if (!_viewModel.LabUnitOptions.Any()) return;

            // Match the selected display string back to a LabUnitOption.
            // Display format: "3.9–6.1  (mmol/L)"  or just "3.9–6.1" for single-unit tests.
            var match = _viewModel.LabUnitOptions.FirstOrDefault(o =>
            {
                var display = string.IsNullOrWhiteSpace(o.Unit)
                    ? o.NormalRange
                    : $"{o.NormalRange}  ({o.Unit})";
                return string.Equals(display, selected, StringComparison.OrdinalIgnoreCase);
            });

            if (match != null)
            {
                // Suppress the unit SelectionChanged re-firing into an infinite loop
                CbLabUnit.SelectionChanged -= CbLabUnit_SelectionChanged;
                _viewModel.LabSelectedUnitOption = match;   // sets both Unit + NormalRange in VM
                CbLabUnit.SelectionChanged += CbLabUnit_SelectionChanged;
            }
        }

        private async void BtnAddNewLabTest_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new AddNewLabTestDialog(_viewModel.LabTestSearchText) { Owner = this };
            if (dlg.ShowDialog() != true) return;
            await _viewModel.AddNewLabTestToCatalogAsync(
                dlg.TestName, dlg.UnitSI, dlg.RangeSI, dlg.UnitImp, dlg.RangeImp);
            // Refresh unit dropdown for the newly added test
            var added = _viewModel.AvailableTests
                .FirstOrDefault(t => t.TestName.Equals(dlg.TestName, StringComparison.OrdinalIgnoreCase));
            if (added != null)
            {
                _viewModel.LabUnitOptions = added.UnitOptions;
                _viewModel.LabSelectedUnitOption = added.UnitOptions.FirstOrDefault();
            }
        }

        // ── Voice dictation ───────────────────────────────────────────────────

        private async void BtnMic_Click(object sender, RoutedEventArgs e)
        {
            if (_dictation.IsListening)
            {
                _dictation.Stop();
                SetMicUiStopped();
            }
            else
            {
                SetMicUiListening();

                // Subscribe once per click so we don't stack handlers
                _dictation.TextAppended  -= OnDictationText;
                _dictation.StatusChanged -= OnDictationStatus;
                _dictation.TextAppended  += OnDictationText;
                _dictation.StatusChanged += OnDictationStatus;

                await _dictation.StartAsync(text =>
                {
                    // Append to Notes directly; binding keeps ViewModel in sync
                    TxtNotes.Text += text;
                    TxtNotes.CaretIndex = TxtNotes.Text.Length;
                });
            }
        }

        private void OnDictationText(string text)
        {
            // Already appended via StartAsync callback; nothing extra needed here
        }

        private void OnDictationStatus(string status)
        {
            bool isListening = status.StartsWith("🎤");
            if (isListening)
                SetMicUiListening(status);
            else
                SetMicUiStopped(status);
        }

        private void SetMicUiListening(string? statusText = null)
        {
            BtnMic.Background  = new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28));  // red
            BtnMic.Foreground  = Brushes.White;
            BtnMic.ToolTip     = "Click to stop dictation";
            DictationStatusBadge.Visibility = Visibility.Visible;
            TxtDictationStatus.Text = statusText ?? "🎤 Listening…";
        }

        private void SetMicUiStopped(string? statusText = null)
        {
            BtnMic.Background = Brushes.Transparent;
            BtnMic.Foreground = Brushes.Black;
            BtnMic.ToolTip    = "Start voice dictation";
            if (string.IsNullOrEmpty(statusText) || statusText.StartsWith("⏹"))
            {
                DictationStatusBadge.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Show brief processing/error status then fade out after 3 seconds
                DictationStatusBadge.Visibility = Visibility.Visible;
                TxtDictationStatus.Text = statusText;
                _ = Task.Delay(3000).ContinueWith(_ =>
                    Dispatcher.Invoke(() => DictationStatusBadge.Visibility = Visibility.Collapsed));
            }
        }

        // ── OCR — lab result image auto-fill ──────────────────────────────────

        private void BtnOcrLabImage_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedPatient == null) return;

            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title  = "Select lab result image",
                Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.tif|All files|*.*",
                Multiselect = true
            };
            if (dlg.ShowDialog(this) != true) return;

            // Show status
            OcrStatusBadge.Visibility = Visibility.Visible;
            TxtOcrStatus.Text = "📷 Saving…";
            BtnOcrLabImage.IsEnabled = false;

            try
            {
                int added = 0;
                foreach (var file in dlg.FileNames)
                {
                    // Create an image-only lab result row (no test name / value required)
                    var imageEntry = new WPF.ViewModels.LabResultLineItem
                    {
                        TestId      = 0,
                        TestName    = "📷 " + System.IO.Path.GetFileNameWithoutExtension(file),
                        ResultValue = string.Empty,
                        Unit        = string.Empty,
                        NormalRange = string.Empty
                    };
                    _viewModel.LabResults = new System.Collections.Generic.List<WPF.ViewModels.LabResultLineItem>(_viewModel.LabResults) { imageEntry };
                    _viewModel.AddAttachmentToLabResult(imageEntry, file);
                    added++;
                }
                TxtOcrStatus.Text = added == 1 ? "✓ Image saved" : $"✓ {added} images saved";
            }
            catch (Exception ex)
            {
                TxtOcrStatus.Text = "✗ " + ex.Message;
            }
            finally
            {
                BtnOcrLabImage.IsEnabled = true;
                _ = System.Threading.Tasks.Task.Delay(3000).ContinueWith(_ =>
                    Dispatcher.Invoke(() => OcrStatusBadge.Visibility = Visibility.Collapsed));
            }
        }

        // ── AI Diagnosis Suggestions ────────────────────────────────────────

        private void VisitTab_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.D &&
                (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) != 0)
            {
                e.Handled = true;
                _ = RunAiSuggestAsync();
            }
        }

        private async void BtnAiSuggest_Click(object sender, RoutedEventArgs e)
            => await RunAiSuggestAsync();

        private async Task RunAiSuggestAsync()
        {
            if (_viewModel.SelectedPatient == null) return;
            if (_viewModel.IsAiThinking) return;

            const string systemPrompt =
                "You are an expert clinical decision support assistant. " +
                "Given the patient data below, provide a concise differential diagnosis. " +
                "Format your response as:\\n" +
                "1. Most likely diagnosis with brief reasoning\\n" +
                "2-4. Other differentials to consider\\n" +
                "Red flags to watch for:\\n" +
                "Suggested next steps:\\n" +
                "Keep it focused and clinically practical. If data is insufficient, state what additional information would help.";

            var context = _viewModel.BuildClinicalContext();

            AiSuggestionsPanel.Visibility = Visibility.Visible;
            _viewModel.IsAiThinking = true;
            _viewModel.AiSuggestions = string.Empty;
            BtnAiSuggest.IsEnabled = false;

            try
            {
                var result = await WPF.Helpers.AiHelper.CompleteAsync(
                    _aiService, context, systemPrompt);

                _viewModel.AiSuggestions = string.IsNullOrWhiteSpace(result)
                    ? "⚠ No response from AI provider. Check Settings → AI to configure a provider."
                    : result.Trim();
            }
            catch (Exception ex)
            {
                _viewModel.AiSuggestions = $"✗ Error: {ex.Message}";
            }
            finally
            {
                _viewModel.IsAiThinking = false;
                BtnAiSuggest.IsEnabled = _viewModel.HasSelectedPatient;
            }
        }

        private void AttachImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is LabResultLineItem item)
            {
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Title  = "Attach image or document",
                    Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.gif|PDF|*.pdf|All files|*.*",
                    Multiselect = true
                };
                if (dlg.ShowDialog(this) != true) return;
                foreach (var f in dlg.FileNames)
                    _viewModel.AddAttachmentToLabResult(item, f);
            }
        }

        private void ScanImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not LabResultLineItem item) return;

            try
            {
                // Open Windows Fax and Scan so the user can scan the document,
                // then prompt them to pick the scanned file to attach.
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("wfs.exe")
                {
                    UseShellExecute = true
                });

                MessageBox.Show(
                    "Windows Fax and Scan has been opened.\n\n" +
                    "1. Scan your document there.\n" +
                    "2. Save it (File → Save As).\n" +
                    "3. Click OK here to pick the saved file.",
                    "Scan Document", MessageBoxButton.OK, MessageBoxImage.Information);

                // Now let the user pick the scanned file
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Title  = "Select scanned file",
                    Filter = "Images & PDFs|*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.tif;*.pdf|All files|*.*",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };
                if (dlg.ShowDialog(this) == true)
                    _viewModel.AddAttachmentToLabResult(item, dlg.FileName);
            }
            catch (Exception)
            {
                // wfs.exe not found — fall back to direct file pick
                var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Title  = "Select image to attach",
                    Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.tif|All files|*.*"
                };
                if (dlg.ShowDialog(this) == true)
                    _viewModel.AddAttachmentToLabResult(item, dlg.FileName);
            }
        }

        private void AttachmentImage_Click(object sender, MouseButtonEventArgs e)
        {
            string? path = null;
            if (sender is Image img && img.Tag is string p) path = p;
            else if (sender is TextBlock tb && tb.Tag is string p2) path = p2;
            if (path == null || !System.IO.File.Exists(path)) return;
            OpenFileOrDicom(path);
        }

        private void RemoveAttachmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not LabAttachment attachment) return;

            // Walk up visual tree to find the LabResultLineItem DataContext
            var parent = btn.Parent as FrameworkElement;
            while (parent != null)
            {
                if (parent.DataContext is LabResultLineItem labItem)
                {
                    _viewModel.RemoveAttachmentFromLabResult(labItem, attachment);
                    return;
                }
                parent = parent.Parent as FrameworkElement;
            }
        }


        // ── Section (Imaging / History) attachment handlers ──────────────────

        private void SectionAttach_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            var section = btn.Tag as string ?? "imaging";

            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title       = section == "imaging" ? "Attach imaging / report" : "Attach history document",
                Filter      = section == "imaging"
                    ? "Images, Reports & DICOM|*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.tif;*.pdf;*.dcm|All files|*.*"
                    : "Documents & Images|*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.tif;*.pdf;*.doc;*.docx|All files|*.*",
                Multiselect = true
            };
            if (dlg.ShowDialog(this) != true) return;

            foreach (var file in dlg.FileNames)
                _viewModel.AddSectionAttachment(section, file);
        }

        private void SectionAttachmentOpen_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var filePath = (sender as FrameworkElement)?.Tag as string;
            if (!string.IsNullOrWhiteSpace(filePath) && System.IO.File.Exists(filePath))
                OpenFileOrDicom(filePath);
        }

        /// <summary>Opens a file: DICOM files get the built-in viewer; everything else uses the OS default.</summary>
        private void OpenFileOrDicom(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !System.IO.File.Exists(filePath)) return;
            var ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            if (ext == ".dcm" || ext == ".dicom")
            {
                var viewer = new WPF.Views.DicomViewerWindow(filePath) { Owner = this };
                viewer.Show();
            }
            else
            {
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
            }
        }

        private void SectionAttachmentRemove_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not LabAttachment att) return;
            // Determine section by checking which collection owns this attachment
            if (_viewModel.ImagingAttachments.Contains(att))
                _viewModel.RemoveSectionAttachment("imaging", att);
            else
                _viewModel.RemoveSectionAttachment("history", att);
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

        private void VisitTab_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            // Slow down scroll: WPF default ~48 px per notch, reduced to 40 px.
            const double scrollAmount = 40;
            double delta = e.Delta > 0 ? -scrollAmount : scrollAmount;
            VisitTabContent.ScrollToVerticalOffset(VisitTabContent.VerticalOffset + delta);
            e.Handled = true;
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

        #region Context Menu Handlers

        private PatientViewModel? GetContextMenuPatient(object sender)
        {
            // The DataContext of the MenuItem's parent ContextMenu's PlacementTarget is the PatientViewModel
            if (sender is MenuItem mi &&
                mi.Parent is ContextMenu cm &&
                cm.PlacementTarget is FrameworkElement fe &&
                fe.DataContext is PatientViewModel patient)
                return patient;
            return null;
        }

        private async void ContextMenu_StartVisit_Click(object sender, RoutedEventArgs e)
        {
            var patient = GetContextMenuPatient(sender);
            if (patient == null) return;
            await _viewModel.SelectPatientAsync(patient, forceNewVisit: true);
        }

        private async void ContextMenu_EditPatient_Click(object sender, RoutedEventArgs e)
        {
            var patient = GetContextMenuPatient(sender);
            if (patient == null) return;
            await EditPatientAsync(patient);
        }

        private async void ContextMenu_DeletePatient_Click(object sender, RoutedEventArgs e)
        {
            var patient = GetContextMenuPatient(sender);
            if (patient == null) return;

            var result = MessageBox.Show(
                $"Delete patient \"{patient.Name}\"?\n\nThis cannot be undone.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            await _viewModel.DeletePatientAsync(patient);
        }

        private async Task EditPatientAsync(PatientViewModel patient)
        {
            try
            {
                var vm = App.Services.GetRequiredService<RegisterPatientViewModel>();

                // Pre-fill with existing values
                vm.Name        = patient.Name;
                vm.DateOfBirth = patient.DateOfBirth;
                vm.Sex         = patient.SexDisplay;
                vm.PhoneNumber = patient.PhoneNumber;
                vm.Address     = patient.Address;
                vm.BloodGroup  = patient.BloodGroup;
                vm.Allergies   = patient.Allergies;

                var dialog = new RegisterPatientWindow(vm) { Owner = this };
                if (dialog.ShowDialog() != true || vm.CreatedPatient == null) return;

                await _viewModel.UpdatePatientAsync(patient.PatientId, vm.CreatedPatient);
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message, "Error Editing Patient");
            }
        }

        #endregion Context Menu Handlers

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








