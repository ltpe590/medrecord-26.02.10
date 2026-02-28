using Core.DTOs;
using Microsoft.Extensions.Logging;
using WPF.Mappers;

namespace WPF.ViewModels
{
    public partial class MainWindowViewModel
    {
        #region Patient Management Operations

        public async Task AddNewPatientAsync(PatientCreateDto patientDto)
        {
            try
            {
                if (string.IsNullOrEmpty(_authToken))
                {
                    _logger.LogWarning("AddNewPatientAsync called without authentication");
                    this.ShowError("Please login first", "Error");
                    return;
                }

                await _patientService.CreatePatientAsync(patientDto);
                await LoadAllPatientsAsync();
                // Don't show success here - caller (ShowNewPatientDialogAsync) handles UI flow
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding patient: {PatientName}", patientDto?.Name);
                this.ShowError(ex.Message, "Add Patient Error");
                throw; // Re-throw so caller knows it failed
            }
        }

        public async Task DeletePatientAsync(PatientViewModel patient)
        {
            try
            {
                _logger.LogInformation("Deleting patient: {PatientId} {Name}", patient.PatientId, patient.Name);
                await _patientService.DeletePatientAsync(patient.PatientId);

                // Deselect if this was the selected patient
                if (SelectedPatient?.PatientId == patient.PatientId)
                    SelectedPatient = null;

                await LoadAllPatientsAsync();
                ShowSuccess($"Patient \"{patient.Name}\" deleted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting patient {PatientId}", patient.PatientId);
                this.ShowError(ex.Message, "Delete Patient Error");
            }
        }

        public async Task UpdatePatientAsync(int patientId, PatientCreateDto dto)
        {
            try
            {
                _logger.LogInformation("Updating patient: {PatientId}", patientId);
                var updateDto = new Core.DTOs.PatientUpdateDto
                {
                    PatientId   = patientId,
                    Name        = dto.Name,
                    DateOfBirth = dto.DateOfBirth,
                    Sex         = dto.Sex,
                    PhoneNumber = dto.PhoneNumber,
                    Address     = dto.Address,
                    BloodGroup  = dto.BloodGroup,
                    Allergies   = dto.Allergies
                };

                await _patientService.UpdatePatientAsync(updateDto);
                await LoadAllPatientsAsync();
                ShowSuccess("Patient updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient {PatientId}", patientId);
                this.ShowError(ex.Message, "Update Patient Error");
            }
        }

        public async Task LoadAllPatientsAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_authToken))
                {
                    StatusMessage = "Please login first";
                    return;
                }

                StatusMessage = "Loading patients...";

                // Use the Core PatientService directly
                var patients = await _patientService.GetAllPatientsListAsync();
                var viewModels = PatientMapper.ToViewModels(patients)
                                              .OrderBy(p => p.Name)
                                              .ToList();

                // Mark patients with a paused visit today
                try
                {
                    var paused = await _visitService.GetPausedVisitsTodayAsync();
                    var pausedIds = paused.Select(p => p.PatientId).ToHashSet();
                    foreach (var vm in viewModels)
                        vm.IsPaused = pausedIds.Contains(vm.PatientId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not load paused visits for patient list");
                }

                Patients.Clear();
                foreach (var vm in viewModels)
                {
                    Patients.Add(vm);
                }

                // Notify that FilteredPatients has changed
                OnPropertyChanged(nameof(FilteredPatients));

                StatusMessage = $"Loaded {Patients.Count} patients";
                PatientsLoaded?.Invoke();
            }
            catch (Exception ex)
            {
                StatusMessage = "Error loading patients";
                _logger.LogError(ex, "Error loading patients");
                OnShowErrorMessage?.Invoke("Load Patients Error", ex.Message);
            }
        }

        public async Task SelectPatientAsync(PatientViewModel? patient, bool forceNewVisit = false)
        {
            _logger.LogInformation("➡️ SelectPatientAsync ENTERED. PatientId={PatientId}, ForceNew={Force}", patient?.PatientId, forceNewVisit);

            if (patient == null)
            {
                ClearPatientSelection();
                return;
            }

            // Only skip if same patient AND we're not forcing a new visit
            if (_lastLoadedPatientId == patient.PatientId && !forceNewVisit)
            {
                _logger.LogInformation("⏭ Same patient already loaded, skipping reload");
                return;
            }

            // If forcing a new visit, reset the current visit ID so StartVisitIfNotAlreadyStarted creates a fresh one
            if (forceNewVisit)
            {
                _currentVisitId = 0;
                _visitStarting = false;
            }

            SelectedPatient = patient;

            try
            {
                IsPatientExpanderOpen = false;
                IsVisitExpanderOpen = true;

                VisitHeaderText = $"Visit – {patient.DisplayName}";

                UpdateSelectedPatientInfo(patient);

                await LoadPatientHistoryAsync(patient.PatientId);
                _logger.LogInformation("▶️ About to call StartVisitIfNotAlreadyStarted. CurrentVisitId={VisitId}", _currentVisitId);

                await StartVisitIfNotAlreadyStarted(patient);

                PatientSelected?.Invoke();
            }
            catch (Exception ex)
            {
                HandlePatientSelectionError(ex, patient);
            }
            finally
            {
                _logger.LogInformation(
                    "⬅️ SelectPatientAsync EXIT. PatientId={PatientId}",
                    patient?.PatientId);
            }
        }

        #endregion Patient Management Operations
    }
}