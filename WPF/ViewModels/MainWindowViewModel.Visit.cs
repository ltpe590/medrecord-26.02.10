using Microsoft.Extensions.Logging;

namespace WPF.ViewModels
{
    public partial class MainWindowViewModel
    {
        #region Visit Operations

        #region Lab Methods

        // AddLabResultAsync removed — replaced by synchronous AddLabResult() in Lab Result Methods region

        private async Task LoadAvailableTestsAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(_authToken))
                {
                    AvailableTests = await _userService.GetTestCatalogAsync(_settings.ApiBaseUrl, _authToken);
                    _logger.LogInformation("Loaded {Count} available tests", AvailableTests.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading test catalog");
            }
        }

        #endregion Lab Methods

        # region Visit Methods

        private async Task StartVisitIfNotAlreadyStarted(PatientViewModel patient)
        {
            _logger.LogInformation(
                "➡️ StartVisitIfNotAlreadyStarted ENTERED. PatientId={PatientId}, CurrentVisitId={VisitId}, VisitStarting={VisitStarting}",
                patient.PatientId, _currentVisitId, _visitStarting);

            if (_currentVisitId > 0)
            {
                _logger.LogInformation(
                    "⏭ Visit already active. VisitId={VisitId}",
                    _currentVisitId);
                return;
            }

            if (_visitStarting)
            {
                _logger.LogWarning("⏭ Visit start already in progress");
                return;
            }

            _visitStarting = true;

            try
            {
                _logger.LogInformation("🚀 Attempting to start visit");

                await StartVisitForPatientAsync(patient, _logger);

                _logger.LogInformation(
                    "✅ Visit started successfully. New VisitId={VisitId}",
                    _currentVisitId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to start visit");
                
                // Show error to user
                OnShowErrorMessage?.Invoke("Visit Start Error",
                    $"Failed to start visit.\n\n{ex.Message}\n\nPlease check that the WebApi is running.");
                    
                throw;
            }
            finally
            {
                _visitStarting = false;
                _logger.LogInformation("⬅️ StartVisitIfNotAlreadyStarted EXIT");
            }
        }

        public async Task StartVisitForPatientAsync(PatientViewModel patient, ILogger<MainWindowViewModel> logger)
        {
            _logger.LogInformation(
                "➡️ StartVisitForPatientAsync ENTERED. PatientId={PatientId}",
                patient?.PatientId);

            try
            {
                // VALIDATION
                if (patient == null)
                {
                    ShowError("No patient selected.", "Error");
                    return;
                }

                if (string.IsNullOrWhiteSpace(_authToken))
                {
                    ShowError("Please login first.", "Authentication Required");
                    return;
                }

                // CREATE NEW VISIT
                _logger.LogInformation(
                    "🆕 No paused visit found. Creating new visit.");

                StatusMessage = $"Starting visit for {patient.Name}...";

                var startResult = await _visitService.StartOrResumeVisitAsync(
                    patient.PatientId,
                    "Visit started",
                    "N/A",
                    "N/A");

                if (startResult.HasPausedVisit)
                {
                    // Resume the paused visit from a previous session
                    await _visitService.ResumeVisitAsync(startResult.PausedVisitId!.Value);
                    _currentVisitId = startResult.PausedVisitId.Value;
                    StatusMessage = $"Resumed paused visit #{_currentVisitId}";
                    ShowSuccess($"Resumed paused visit #{_currentVisitId} for {patient.Name}");
                }
                else
                {
                    _currentVisitId = startResult.VisitId;
                    StatusMessage = $"Visit #{_currentVisitId} started";
                    ShowSuccess($"Visit #{_currentVisitId} started for {patient.Name}");
                }

                VisitHeaderText = $"Visit – {patient.DisplayName}";
                _logger.LogInformation("Visit started. VisitId={VisitId}", _currentVisitId);

                // Initialize VisitPageViewModel with the active visit context
                await CurrentVisit!.BeginVisitAsync(_currentVisitId, patient.PatientId, patient.DisplayName, _authToken);

                try { await LoadAvailableTestsAsync(); }
                catch (Exception testEx) { _logger.LogWarning(testEx, "Failed to load test catalog"); }
            }
            catch (Exception ex)
            {
                StatusMessage = "Error starting visit";

                _logger.LogError(
                    ex,
                    "❌ Error starting visit for PatientId={PatientId}",
                    patient?.PatientId);

                // Re-throw so SelectPatientAsync catch block can handle showing the error
                throw;
            }
        }

        public async Task PauseVisitAsync()
        {
            if (CurrentVisit == null)
            {
                ShowError("No active visit to pause.", "Error");
                return;
            }
            try
            {
                await CurrentVisit.PauseVisitAsync();
                ShowSuccess("Visit paused.");
                _logger.LogInformation("Visit paused");
                _currentVisitId = 0;
                _visitStarting = false;
                CurrentVisit = null;
                StatusMessage = "Visit paused";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pausing visit {VisitId}", _currentVisitId);
                ShowError($"Error pausing visit: {ex.Message}", "Pause Error");
            }
        }

        public async Task CompleteVisitAsync()
        {
            if (_currentVisitId == 0 || SelectedPatient == null || CurrentVisit == null)
            {
                ShowError("No active visit to complete.", "Error");
                return;
            }
            try
            {
                var completedPatientId = SelectedPatient.PatientId;
                await CurrentVisit.CompleteVisitAsync();
                ShowSuccess("Visit completed.");
                ClearVisitFormAndVisit();
                await LoadPatientHistoryAsync(completedPatientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing visit");
                ShowError($"Error completing visit: {ex.Message}", "Complete Error");
            }
        }

        public async Task SaveVisitAsync()
        {
            _logger.LogInformation("=== SaveVisitAsync CALLED (delegating to CurrentVisit) ===");

            if (CurrentVisit == null || _currentVisitId == 0 || SelectedPatient == null)
            {
                ShowError("No active visit to save.", "Error");
                return;
            }

            try
            {
                var savedPatientId = SelectedPatient.PatientId;
                await CurrentVisit.SaveVisitAsync();
                ShowSuccess("Visit saved successfully");
                ClearVisitFormAndVisit();
                await LoadPatientHistoryAsync(savedPatientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving visit {VisitId}", _currentVisitId);
                ShowError($"Error saving visit: {ex.Message}", "Save Error");
            }

            SaveVisitRequested?.Invoke();
        }

        /// <summary>Resets CurrentVisit and clears the visit tracking fields on this ViewModel.</summary>
        private void ClearVisitFormAndVisit()
        {
            CurrentVisit?.ClearVisitForm();
            _currentVisitId = 0;
            _visitStarting  = false;
            StatusMessage   = "Ready";
        }

        private void ClearVisitForm()
        {
            Diagnosis = string.Empty;
            Notes = string.Empty;
            PresentingSymptoms    = string.Empty;
            HistoryAndExamination = string.Empty;
            ImagingFindings       = string.Empty;
            AiSuggestions         = string.Empty;
            ImagingAttachments = new List<LabAttachment>();
            HistoryAttachments = new List<LabAttachment>();
            Temperature = 0;
            BPSystolic = 0;
            BPDiastolic = 0;
            Gravida = 0;
            Para = 0;
            Abortion = 0;
            LabResultValue = string.Empty;
            SelectedTest = null;
            Prescriptions = new List<PrescriptionLineItem>();
            ClearRxInputs();
            LabResults = new List<LabResultLineItem>();
            ClearLabInputs();

            // Reset visit ID after saving
            _currentVisitId = 0;
            StatusMessage = "Ready";
        }

        /// <summary>
        /// Assembles all current visit data into a structured clinical context string
        /// suitable for sending to an AI model for differential diagnosis assistance.
        /// </summary>
        public string BuildClinicalContext()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== PATIENT CLINICAL CONTEXT ===");
            sb.AppendLine();

            // Patient demographics
            if (SelectedPatient != null)
            {
                sb.AppendLine($"Patient: {SelectedPatient.Name}");
                sb.AppendLine($"Age: {SelectedPatient.Age} years");
                sb.AppendLine($"Sex: {SelectedPatient.SexDisplay}");
                if (!string.IsNullOrWhiteSpace(SelectedPatient.BloodGroup))
                    sb.AppendLine($"Blood Group: {SelectedPatient.BloodGroup}");
                if (!string.IsNullOrWhiteSpace(SelectedPatient.Allergies))
                    sb.AppendLine($"Known Allergies: {SelectedPatient.Allergies}");
            }

            // Vitals
            sb.AppendLine();
            sb.AppendLine("VITALS:");
            if (Temperature > 0)  sb.AppendLine($"  Temperature: {Temperature} °C");
            if (BPSystolic > 0)   sb.AppendLine($"  Blood Pressure: {BPSystolic}/{BPDiastolic} mmHg");

            // Clinical narrative fields
            if (!string.IsNullOrWhiteSpace(PresentingSymptoms))
            {
                sb.AppendLine();
                sb.AppendLine("PRESENTING SYMPTOMS:");
                sb.AppendLine(PresentingSymptoms.Trim());
            }
            if (!string.IsNullOrWhiteSpace(HistoryAndExamination))
            {
                sb.AppendLine();
                sb.AppendLine("HISTORY AND EXAMINATION:");
                sb.AppendLine(HistoryAndExamination.Trim());
            }
            if (!string.IsNullOrWhiteSpace(ImagingFindings))
            {
                sb.AppendLine();
                sb.AppendLine("IMAGING / INVESTIGATIONS:");
                sb.AppendLine(ImagingFindings.Trim());
            }
            if (!string.IsNullOrWhiteSpace(Diagnosis))
            {
                sb.AppendLine();
                sb.AppendLine("WORKING DIAGNOSIS:");
                sb.AppendLine(Diagnosis.Trim());
            }
            if (!string.IsNullOrWhiteSpace(Notes))
            {
                sb.AppendLine();
                sb.AppendLine("NOTES:");
                sb.AppendLine(Notes.Trim());
            }

            // Lab results
            if (LabResults.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("LAB RESULTS:");
                foreach (var r in LabResults)
                {
                    var ref_ = string.IsNullOrWhiteSpace(r.NormalRange) ? "" : $" (ref: {r.NormalRange})";
                    sb.AppendLine($"  {r.TestName}: {r.ResultValue} {r.Unit}{ref_}");
                }
            }

            return sb.ToString();
        }

        public void PrintVisitSummary(PatientViewModel? selectedPatient)
        {
            if (selectedPatient == null)
            {
                ShowError("Please select a patient first.", "No Patient Selected");
                return;
            }

            if (string.IsNullOrWhiteSpace(Diagnosis))
            {
                ShowWarning("Diagnosis field is empty.", "Warning");
            }

            try
            {
                var summary = BuildVisitSummary(selectedPatient);
                SaveSummaryToFile(selectedPatient, summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing visit summary");
                ShowError($"Error creating summary: {ex.Message}", "Print Error");
            }
        }

        # endregion Visit Methods

        #endregion Visit Operations
    }
}