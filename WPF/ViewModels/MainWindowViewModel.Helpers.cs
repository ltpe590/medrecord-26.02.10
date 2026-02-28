using Core.DTOs;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Windows;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace WPF.ViewModels
{
    public partial class MainWindowViewModel
    {
        #region Helper Methods

        #region Patient Helpers

        private void ClearPatientSelection()
        {
            SelectedPatient = null;
            SelectedPatientInfo = string.Empty;
            SelectedPatientDetails = string.Empty;
            PatientHistory = "No patient selected";
            _lastLoadedPatientId = 0;
        }

        private void UpdateSelectedPatientInfo(PatientViewModel patient)
        {
            SelectedPatient = patient;
            SelectedPatientInfo = patient.Name;
            SelectedPatientDetails = $"Age: {patient.Age}, Sex: {patient.SexDisplay}";
        }

        private async Task LoadPatientHistoryAsync(int patientId)
        {
            if (string.IsNullOrEmpty(_authToken))
            {
                PatientHistory = "Please login to view history";
                return;
            }

            try
            {
                var visits = await _visitService.GetVisitHistoryForPatientAsync(patientId);
                var mapper = _visitMapper;

                PatientHistory = visits?.Count > 0
                    ? string.Join("\n\n", visits.Select(v => mapper.ToDisplayString(v)))
                    : "No visit history.";

                _lastLoadedPatientId = patientId;
            }
            catch (Exception ex)
            {
                PatientHistory = "Error loading history";
                _logger.LogError(ex, "Error loading patient history");
            }
        }

        private void HandlePatientSelectionError(Exception ex, PatientViewModel patient)
        {
            PatientHistory = "Failed to load visit history.";
            _logger.LogError(ex, "Error selecting patient {PatientId}", patient.PatientId);
            
            // Show error to user
            OnShowErrorMessage?.Invoke("Visit Start Error", 
                $"Failed to start visit for {patient.Name}.\n\n{ex.Message}\n\nPlease check that the WebApi is running and accessible.");
        }

        #endregion Patient Helpers

        #region Auth Helpers

        private void ValidateAuthToken()
        {
            if (string.IsNullOrEmpty(_authToken))
            {
                OnShowErrorMessage?.Invoke("Error", "Please login first");
                throw new UnauthorizedAccessException("User is not authenticated");
            }
        }

        private async Task ReloadIfAuthenticated()
        {
            if (!string.IsNullOrEmpty(_authToken))
                await LoadAllPatientsAsync();
        }

        #endregion Auth Helpers

        #region Error Helpers

        private void ShowError(string message, string title = "Error")
        {
            Application.Current.Dispatcher.Invoke(() =>
                OnShowErrorMessage?.Invoke(title, message));
        }

        private void ShowSuccess(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
                OnShowSuccessMessage?.Invoke(message));
        }

        private void ShowWarning(string message, string title = "Warning")
        {
            Application.Current.Dispatcher.Invoke(() =>
                OnShowWarningMessage?.Invoke(title, message));
        }

        private void ShowInfo(string message, string title = "Information")
        {
            Application.Current.Dispatcher.Invoke(() =>
                OnShowInfoMessage?.Invoke(title, message));
        }

        #endregion Error Helpers

        #region Visit Helpers

        #region Visit Summary

        private string BuildVisitSummary(PatientViewModel patient)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Patient: {patient.Name}");
            sb.AppendLine($"DOB: {patient.DateOfBirth:dd MMM yyyy}");
            sb.AppendLine($"Age: {patient.Age}");
            sb.AppendLine($"Sex: {patient.Sex}");
            sb.AppendLine();
            sb.AppendLine($"Diagnosis:");
            sb.AppendLine(Diagnosis);
            sb.AppendLine();
            sb.AppendLine($"Notes:");
            sb.AppendLine(Notes);

            // Add lab results if available
            if (!string.IsNullOrWhiteSpace(LabResultValue) && SelectedTest != null)
            {
                sb.AppendLine();
                sb.AppendLine($"Lab Result:");
                sb.AppendLine($"{SelectedTest.TestName}: {LabResultValue}");
            }

            return sb.ToString();
        }

        private void SaveSummaryToFile(PatientViewModel patient, string summary)
        {
            var TEMP_FOLDER = Path.Combine(Path.GetTempPath(), "MedRecord");
            Directory.CreateDirectory(TEMP_FOLDER);

            // Sanitize filename
            var fileName = $"{patient.Name.Replace(" ", "_")}_VisitSummary_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var c in invalidChars)
            {
                fileName = fileName.Replace(c.ToString(), "");
            }

            var path = Path.Combine(TEMP_FOLDER, fileName);
            System.IO.File.WriteAllText(path, summary);

            ShowInfo($"Summary saved to:\n{path}", "Summary Saved");
        }

        #endregion Visit Summary

        #region Paused Visits Helpers

        public async Task LoadPausedVisitsAsync()
        {
            try
            {
                _logger.LogInformation("Loading paused visits...");
                
                // Properly await the async method instead of blocking
                var pausedVisits = await _visitService.GetPausedVisitsTodayAsync();

                _logger.LogInformation("Loaded {Count} paused visits", pausedVisits?.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading paused visits");
            }
        }

        #endregion Paused Visits Helpers

        #region Prescription Methods

        public void AddPrescription()
        {
            if (!CanAddPrescription) return;

            var matched = _availableDrugs.FirstOrDefault(
                d => string.Equals(d.BrandName, RxDrugText.Trim(), StringComparison.OrdinalIgnoreCase));

            var entry = new PrescriptionLineItem
            {
                DrugId       = matched?.DrugId ?? 0,
                DrugName     = matched?.BrandName ?? RxDrugText.Trim(),
                Form         = matched?.Form,
                Dose         = RxDose.Trim(),
                Route        = RxRoute,
                Frequency    = RxFrequency,
                DurationDays = RxDuration.Trim(),
                Instructions = RxInstructions.Trim()
            };

            // ── Remember these choices as "last used" for this drug ──────────
            _lastUsedRx[entry.DrugName] = entry;

            var updated = new List<PrescriptionLineItem>(Prescriptions) { entry };
            Prescriptions = updated;
            ClearRxInputs();
            StatusMessage = "Prescription added";
        }

        public void RemovePrescription(PrescriptionLineItem item)
        {
            var updated = new List<PrescriptionLineItem>(Prescriptions);
            updated.Remove(item);
            Prescriptions = updated;
            StatusMessage = "Prescription removed";
        }

        /// <summary>
        /// Called when the user picks a drug from the autocomplete list.
        /// Fills dose, route, frequency, duration, and instructions using:
        ///   1. Last-used values for this drug (highest priority)
        ///   2. Catalog-stored defaults (Route, Frequency, Instructions on DrugCatalogDto)
        ///   3. FormDefaults keyed by drug Form (e.g. Tablet → Oral / BID / 7 days)
        /// Only overwrites a field if it is currently empty, so the user
        /// can still type first and then pick without losing their input.
        /// Pass <paramref name="overwrite"/> = true to always replace all fields.
        /// </summary>
        public void AutoFillFromDrug(DrugCatalogDto drug, bool overwrite = false)
        {
            RxDrugText = drug.BrandName;

            // Tier 1 – last used
            _lastUsedRx.TryGetValue(drug.BrandName, out var last);

            // Tier 3 – form-based defaults (fallback)
            FormDefaults.TryGetValue(drug.Form ?? string.Empty, out var formDef);

            string? Best(string? lastVal, string? catalogVal, string? formVal)
            {
                if (!string.IsNullOrWhiteSpace(lastVal))    return lastVal;
                if (!string.IsNullOrWhiteSpace(catalogVal)) return catalogVal;
                if (!string.IsNullOrWhiteSpace(formVal))    return formVal;
                return null;
            }

            var dose         = Best(last?.Dose,         drug.DosageStrength, null);
            var route        = Best(last?.Route,        drug.Route,          formDef.Route);
            var frequency    = Best(last?.Frequency,    drug.Frequency,      formDef.Frequency);
            var duration     = Best(last?.DurationDays, null,                formDef.Duration);
            var instructions = Best(last?.Instructions, drug.Instructions,   null);

            if (overwrite || string.IsNullOrWhiteSpace(RxDose))         RxDose         = dose         ?? string.Empty;
            if (overwrite || string.IsNullOrWhiteSpace(RxRoute))        RxRoute        = route        ?? string.Empty;
            if (overwrite || string.IsNullOrWhiteSpace(RxFrequency))    RxFrequency    = frequency    ?? string.Empty;
            if (overwrite || string.IsNullOrWhiteSpace(RxDuration))     RxDuration     = duration     ?? string.Empty;
            if (overwrite || string.IsNullOrWhiteSpace(RxInstructions)) RxInstructions = instructions ?? string.Empty;

            // Show where the values came from
            StatusMessage = last != null
                ? $"✓ Auto-filled from last use of {drug.BrandName}"
                : !string.IsNullOrWhiteSpace(route) || !string.IsNullOrWhiteSpace(dose)
                    ? $"✓ Auto-filled defaults for {drug.BrandName}"
                    : $"Selected: {drug.BrandName}";
        }

        public async Task AddNewDrugAndPrescribeAsync(string brandName, string? form, string? strength,
            string? route = null, string? frequency = null, string? instructions = null)
        {
            if (string.IsNullOrWhiteSpace(brandName)) return;

            try
            {
                StatusMessage = "Saving new drug to catalog...";

                using var http = new HttpClient();
                http.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authToken);

                var dto = new
                {
                    BrandName     = brandName.Trim(),
                    Form          = form?.Trim(),
                    DosageStrength= strength?.Trim(),
                    Route         = route?.Trim(),
                    Frequency     = frequency?.Trim(),
                    Instructions  = instructions?.Trim()
                };
                var json    = System.Text.Json.JsonSerializer.Serialize(dto);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var resp    = await http.PostAsync($"{_settings.ApiBaseUrl}/api/DrugCatalogs", content);
                resp.EnsureSuccessStatusCode();

                // Refresh catalog
                AvailableDrugs = await _userService.GetDrugCatalogAsync(_settings.ApiBaseUrl, _authToken);

                // Pre-fill the drug name field so user just fills in dose details
                var newDrug = AvailableDrugs.FirstOrDefault(
                    d => string.Equals(d.BrandName, brandName.Trim(), StringComparison.OrdinalIgnoreCase));

                RxDrugText = newDrug?.BrandName ?? brandName.Trim();
                if (newDrug != null)
                    AutoFillFromDrug(newDrug, overwrite: true);
                else if (!string.IsNullOrWhiteSpace(strength))
                    RxDose = strength;

                StatusMessage = $"\u2713 '{brandName}' added to catalog \u2014 fill dose details and click Add";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save new drug to catalog");
                ShowError($"Could not save drug to catalog:\n{ex.Message}", "Catalog Error");
            }
        }

        private async Task LoadDrugCatalogAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(_authToken))
                    AvailableDrugs = await _userService.GetDrugCatalogAsync(_settings.ApiBaseUrl, _authToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load drug catalog");
            }
        }

        public void ClearRxInputs()
        {
            RxDrugText    = string.Empty;
            RxDose        = string.Empty;
            RxRoute       = string.Empty;
            RxFrequency   = string.Empty;
            RxDuration    = string.Empty;
            RxInstructions = string.Empty;
        }

        #endregion Prescription Methods

        #region Lab Result Methods

        public void AddLabResult()
        {
            if (!CanAddLabItem) return;

            var matched = _availableTests.FirstOrDefault(
                t => string.Equals(t.TestName, LabTestSearchText.Trim(), StringComparison.OrdinalIgnoreCase));

            var entry = new LabResultLineItem
            {
                TestId      = matched?.TestId ?? 0,
                TestName    = matched?.TestName ?? LabTestSearchText.Trim(),
                ResultValue = LabResultValue.Trim(),
                Unit        = string.IsNullOrWhiteSpace(LabResultUnit) ? (matched?.TestUnit ?? string.Empty) : LabResultUnit.Trim(),
                NormalRange = string.IsNullOrWhiteSpace(LabNormalRange) ? (matched?.NormalRange ?? string.Empty) : LabNormalRange.Trim(),
                Notes       = LabResultNotes.Trim(),
                UnitOptions = matched?.UnitOptions ?? new List<Core.DTOs.LabUnitOption>()
            };

            LabResults = new List<LabResultLineItem>(LabResults) { entry };
            ClearLabInputs();
            StatusMessage = "Lab result added";
        }

        public void RemoveLabResult(LabResultLineItem item)
        {
            LabResults = LabResults.Where(x => x != item).ToList();
            StatusMessage = "Lab result removed";
        }

        /// <summary>
        /// Saves all current lab results for the given visitId to the API.
        /// Deletes any previously saved results first (replace-all approach),
        /// then posts each item with its visit-specific Unit and NormalRange.
        /// </summary>
        private async Task SaveLabResultsAsync(int visitId)
        {
            if (LabResults.Count == 0) return;

            using var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authToken);

            // Delete existing results for this visit so we don't accumulate duplicates on re-save
            await http.DeleteAsync($"{_settings.ApiBaseUrl}/api/LabResults/visit/{visitId}");

            // Post each result with the unit + normal range chosen at time of this visit
            foreach (var lab in LabResults)
            {
                var dto = new
                {
                    TestId      = lab.TestId,
                    VisitId     = visitId,
                    ResultValue = lab.ResultValue,
                    Unit        = lab.Unit,
                    NormalRange = lab.NormalRange,
                    Notes       = lab.Notes
                };
                var json    = System.Text.Json.JsonSerializer.Serialize(dto);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var resp    = await http.PostAsync($"{_settings.ApiBaseUrl}/api/LabResults", content);
                resp.EnsureSuccessStatusCode();
            }
        }

        public void AddSectionAttachment(string section, string filePath)
        {
            if (!System.IO.File.Exists(filePath)) return;
            var att = BuildAttachment(filePath);
            if (section == "imaging")
            {
                ImagingAttachments = new List<LabAttachment>(_imagingAttachments) { att };
                StatusMessage = $"Imaging: attached {att.FileName}";
            }
            else
            {
                HistoryAttachments = new List<LabAttachment>(_historyAttachments) { att };
                StatusMessage = $"History: attached {att.FileName}";
            }
        }

        public void RemoveSectionAttachment(string section, LabAttachment att)
        {
            if (section == "imaging")
                ImagingAttachments = _imagingAttachments.Where(a => a != att).ToList();
            else
                HistoryAttachments = _historyAttachments.Where(a => a != att).ToList();
        }

        // Thumbnail loading is intentionally absent here.
        // The FilePathToThumbnailConverter in the view layer handles it on demand.
        private static LabAttachment BuildAttachment(string filePath) =>
            new() { FilePath = filePath };

        public void AddAttachmentToLabResult(LabResultLineItem item, string filePath)
        {
            if (!System.IO.File.Exists(filePath)) return;
            var attachment = BuildAttachment(filePath);
            item.AddAttachment(attachment);
            LabResults    = new List<LabResultLineItem>(LabResults);
            StatusMessage = $"Attached: {attachment.FileName}";
        }

        public void RemoveAttachmentFromLabResult(LabResultLineItem item, LabAttachment attachment)
        {
            item.RemoveAttachment(attachment);
            LabResults = new List<LabResultLineItem>(LabResults);
        }

        private void ClearLabInputs()
        {
            LabTestSearchText    = string.Empty;
            LabResultValue       = string.Empty;
            LabResultUnit        = string.Empty;
            LabResultNotes       = string.Empty;
            LabNormalRange       = string.Empty;
            LabSelectedTestId    = 0;
            LabUnitOptions       = new List<Core.DTOs.LabUnitOption>();
            LabSelectedUnitOption= null;
        }

        public async Task AddNewLabTestToCatalogAsync(string testName,
            string unitSI, string rangeSI, string unitImp, string rangeImp)
        {
            if (string.IsNullOrWhiteSpace(testName)) return;
            try
            {
                StatusMessage = "Saving new test to catalog...";
                using var http = new HttpClient();
                http.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authToken);

                var dto = new
                {
                    TestName            = testName.Trim(),
                    TestUnit            = unitSI.Trim(),
                    NormalRange         = rangeSI.Trim(),
                    UnitImperial        = string.IsNullOrWhiteSpace(unitImp)  ? null : unitImp.Trim(),
                    NormalRangeImperial = string.IsNullOrWhiteSpace(rangeImp) ? null : rangeImp.Trim()
                };
                var content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(dto),
                    System.Text.Encoding.UTF8, "application/json");
                var resp = await http.PostAsync($"{_settings.ApiBaseUrl}/api/TestCatalogs", content);
                resp.EnsureSuccessStatusCode();

                // Refresh local catalog
                AvailableTests = await _userService.GetTestCatalogAsync(_settings.ApiBaseUrl, _authToken);
                LabTestSearchText = testName.Trim();
                if (!string.IsNullOrWhiteSpace(unitSI))   LabResultUnit = unitSI;
                else if (!string.IsNullOrWhiteSpace(unitImp)) LabResultUnit = unitImp;

                StatusMessage = $"✓ '{testName}' added to catalog";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add test to catalog");
                ShowError($"Could not save test:\n{ex.Message}", "Catalog Error");
            }
        }

        #endregion Lab Result Methods

        #endregion Visit Helpers


        #endregion Helper Methods
    }
}
