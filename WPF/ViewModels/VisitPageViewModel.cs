using Core.AI;
using Core.DTOs;
using WPF.Commands;
using WPF.Helpers;
using WPF.Services;
using System.Windows.Input;
using Core.Http;
using Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;

namespace WPF.ViewModels
{
    public class VisitPageViewModel : BaseViewModel
    {
        #region Fields
        private readonly IAppSettingsService _settings;
        private readonly IUserService _userService;
        private readonly IVisitService _visitService;
        private readonly ILogger<VisitPageViewModel> _logger;
        private int _patientId;
        private string _patientName = string.Empty;
        private int _currentVisitId;
        private DateTime _visitStartTime;
        private DateTime? _lastSavedTime;
        private string _statusMessage = string.Empty;
        private string _visitHeaderText = string.Empty;
        private string _diagnosis = string.Empty;
        private string _notes = string.Empty;
        private string _presentingSymptoms = string.Empty;
        private string _historyAndExamination = string.Empty;
        private string _imagingFindings = string.Empty;
        private string _aiSuggestions = string.Empty;
        private bool _isAiThinking;
        private decimal _temperature;
        private int _bpSystolic, _bpDiastolic, _gravida, _para, _abortion;
        private DateTime? _lmpDate;
        private List<LabAttachment> _imagingAttachments = new();
        private List<LabAttachment> _historyAttachments = new();
        private string _labTestSearchText = string.Empty;
        private string _labResultValue = string.Empty;
        private string _labResultUnit = string.Empty;
        private string _labResultNotes = string.Empty;
        private string _labNormalRange = string.Empty;
        private int _labSelectedTestId;
        private List<LabUnitOption> _labUnitOptions = new();
        private LabUnitOption? _labSelectedUnitOption;
        private List<TestCatalogDto> _availableTests = new();
        private List<DrugCatalogDto> _availableDrugs = new();
        private List<LabResultLineItem> _labResults = new();
        private List<PrescriptionLineItem> _prescriptions = new();
        private string _rxDrugText = string.Empty;
        private string _rxDose = string.Empty;
        private string _rxRoute = string.Empty;
        private string _rxFrequency = string.Empty;
        private string _rxDuration = string.Empty;
        private string _rxInstructions = string.Empty;
        private readonly Dictionary<string, PrescriptionLineItem> _lastUsedRx =
            new(StringComparer.OrdinalIgnoreCase);
        private string _authToken = string.Empty;

        // ── Patient context (set during BeginVisitAsync for AI suggest) ────────
        private int     _patientAge;
        private string  _patientSex        = string.Empty;
        private string? _patientBloodGroup;

        // ── Services for AI + dictation ──────────────────────────────────────
        private readonly Core.AI.IAiService          _ai;
        private readonly VoiceDictationService       _dictation;
        private bool                                  _isDictating;

        /// <summary>Raised when the VM needs to show an error. Subscribed by VisitTabControl.xaml.cs.</summary>
        public event Action<string, string>? OnShowError;
        #endregion

        #region Constructor
        public VisitPageViewModel(
            IAppSettingsService           settings,
            IUserService                  userService,
            IVisitService                 visitService,
            ILogger<VisitPageViewModel>   logger,
            Core.AI.IAiService            ai,
            VoiceDictationService         dictation)
        {
            _settings     = settings;
            _userService  = userService;
            _visitService = visitService;
            _logger       = logger;
            _ai           = ai;
            _dictation    = dictation;

            AiSuggestCommand       = new AsyncRelayCommand(RunAiSuggestAsync, () => !IsAiThinking && HasActiveVisit);
            ToggleDictationCommand = new RelayCommand(ToggleDictation);
        }

        // -- Commands
        public System.Windows.Input.ICommand AiSuggestCommand       { get; }
        public System.Windows.Input.ICommand ToggleDictationCommand { get; }

        // -- Print context (settings injected, no service locator)
        public string PrintClinicName  => _settings.ClinicName;
        public string PrintDoctorLine  => $"{_settings.DoctorTitle} {_settings.DoctorName}";
        public string PrintClinicPhone => _settings.ClinicPhone;

        // -- Dictation state
        public bool IsDictating
        {
            get => _isDictating;
            private set => SetProperty(ref _isDictating, value);
        }
        #endregion

        #region Visit State Properties
        public int CurrentVisitId { get => _currentVisitId; private set => SetProperty(ref _currentVisitId, value); }
        public bool HasActiveVisit => _currentVisitId > 0;
        public string VisitHeaderText { get => _visitHeaderText; set => SetProperty(ref _visitHeaderText, value); }
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
        public string LastSavedText => _lastSavedTime?.ToString("HH:mm:ss") ?? "Not saved";
        #endregion

        #region Clinical Properties
        public string Diagnosis
        {
            get => _diagnosis;
            set { SetProperty(ref _diagnosis, value); OnPropertyChanged(nameof(CanSaveVisit)); }
        }
        public string Notes { get => _notes; set => SetProperty(ref _notes, value); }
        public string PresentingSymptoms { get => _presentingSymptoms; set => SetProperty(ref _presentingSymptoms, value); }
        public string HistoryAndExamination { get => _historyAndExamination; set => SetProperty(ref _historyAndExamination, value); }
        public string ImagingFindings { get => _imagingFindings; set => SetProperty(ref _imagingFindings, value); }
        public string AiSuggestions { get => _aiSuggestions; set => SetProperty(ref _aiSuggestions, value); }
        public bool IsAiThinking { get => _isAiThinking; set => SetProperty(ref _isAiThinking, value); }
        public decimal Temperature { get => _temperature; set => SetProperty(ref _temperature, value); }
        public int BPSystolic { get => _bpSystolic; set => SetProperty(ref _bpSystolic, value); }
        public int BPDiastolic { get => _bpDiastolic; set => SetProperty(ref _bpDiastolic, value); }
        public int Gravida { get => _gravida; set => SetProperty(ref _gravida, value); }
        public int Para { get => _para; set => SetProperty(ref _para, value); }
        public int Abortion { get => _abortion; set => SetProperty(ref _abortion, value); }
        public DateTime? LMPDate
        {
            get => _lmpDate;
            set { SetProperty(ref _lmpDate, value); OnPropertyChanged(nameof(EDDText)); }
        }
        public string EDDText => LMPDate.HasValue ? $"EDD: {LMPDate.Value.AddDays(280):dd MMM yyyy}" : string.Empty;
        public bool CanSaveVisit => _currentVisitId > 0 && !string.IsNullOrWhiteSpace(Diagnosis);
        #endregion

        #region Attachment Properties
        public List<LabAttachment> ImagingAttachments
        {
            get => _imagingAttachments;
            set { SetProperty(ref _imagingAttachments, value); OnPropertyChanged(nameof(HasImagingAttachments)); }
        }
        public bool HasImagingAttachments => _imagingAttachments.Count > 0;
        public List<LabAttachment> HistoryAttachments
        {
            get => _historyAttachments;
            set { SetProperty(ref _historyAttachments, value); OnPropertyChanged(nameof(HasHistoryAttachments)); }
        }
        public bool HasHistoryAttachments => _historyAttachments.Count > 0;
        #endregion

        #region Lab Result Properties
        public List<TestCatalogDto> AvailableTests { get => _availableTests; set => SetProperty(ref _availableTests, value); }
        public List<LabResultLineItem> LabResults { get => _labResults; set => SetProperty(ref _labResults, value); }
        public string LabTestSearchText
        {
            get => _labTestSearchText;
            set { SetProperty(ref _labTestSearchText, value); OnPropertyChanged(nameof(CanAddLabItem)); }
        }
        public string LabResultValue
        {
            get => _labResultValue;
            set { SetProperty(ref _labResultValue, value); OnPropertyChanged(nameof(CanAddLabItem)); }
        }
        public string LabResultUnit { get => _labResultUnit; set => SetProperty(ref _labResultUnit, value); }
        public string LabResultNotes { get => _labResultNotes; set => SetProperty(ref _labResultNotes, value); }
        public string LabNormalRange { get => _labNormalRange; set => SetProperty(ref _labNormalRange, value); }
        public int LabSelectedTestId
        {
            get => _labSelectedTestId;
            set { SetProperty(ref _labSelectedTestId, value); OnPropertyChanged(nameof(CanAddLabItem)); }
        }
        public List<LabUnitOption> LabUnitOptions
        {
            get => _labUnitOptions;
            set { SetProperty(ref _labUnitOptions, value); OnPropertyChanged(nameof(LabNormalRangeOptions)); }
        }
        public List<string> LabNormalRangeOptions =>
            _labUnitOptions.Where(o => !string.IsNullOrWhiteSpace(o.NormalRange))
                .Select(o => string.IsNullOrWhiteSpace(o.Unit) ? o.NormalRange : $"{o.NormalRange}  ({o.Unit})")
                .ToList();
        public LabUnitOption? LabSelectedUnitOption
        {
            get => _labSelectedUnitOption;
            set
            {
                SetProperty(ref _labSelectedUnitOption, value);
                if (value != null) { LabResultUnit = value.Unit; LabNormalRange = value.NormalRange; }
            }
        }
        public bool CanAddLabItem =>
            !string.IsNullOrWhiteSpace(LabTestSearchText) && !string.IsNullOrWhiteSpace(LabResultValue);
        #endregion

        #region Prescription Properties
        public List<DrugCatalogDto> AvailableDrugs { get => _availableDrugs; set => SetProperty(ref _availableDrugs, value); }
        public List<PrescriptionLineItem> Prescriptions { get => _prescriptions; set => SetProperty(ref _prescriptions, value); }
        public string RxDrugText
        {
            get => _rxDrugText;
            set { SetProperty(ref _rxDrugText, value); OnPropertyChanged(nameof(CanAddPrescription)); }
        }
        public string RxDose
        {
            get => _rxDose;
            set { SetProperty(ref _rxDose, value); OnPropertyChanged(nameof(CanAddPrescription)); }
        }
        public string RxRoute { get => _rxRoute; set => SetProperty(ref _rxRoute, value); }
        public string RxFrequency { get => _rxFrequency; set => SetProperty(ref _rxFrequency, value); }
        public string RxDuration { get => _rxDuration; set => SetProperty(ref _rxDuration, value); }
        public string RxInstructions { get => _rxInstructions; set => SetProperty(ref _rxInstructions, value); }
        public bool CanAddPrescription => !string.IsNullOrWhiteSpace(RxDrugText) && !string.IsNullOrWhiteSpace(RxDose);
        public static IReadOnlyList<string> RouteOptions { get; } =
            new[] { "Oral", "IV", "IM", "SC", "Topical", "Inhaled", "Sublingual", "Rectal", "Ophthalmic", "Otic", "Nasal" };
        public static IReadOnlyList<string> FrequencyOptions { get; } =
            new[] { "Once daily", "Twice daily (BID)", "Three times daily (TID)", "Four times daily (QID)",
                    "Every 8 hours", "Every 6 hours", "Every 12 hours",
                    "At bedtime (QHS)", "As needed (PRN)", "Stat (immediately)" };
        public static readonly Dictionary<string, (string Route, string Frequency, string Duration)> FormDefaults =
            new(StringComparer.OrdinalIgnoreCase)
        {
            ["Tablet"] = ("Oral","Twice daily (BID)","7"), ["Capsule"] = ("Oral","Twice daily (BID)","7"),
            ["Syrup"] = ("Oral","Three times daily (TID)","5"), ["Suspension"] = ("Oral","Twice daily (BID)","7"),
            ["Injection"] = ("IM","Once daily","3"), ["Cream"] = ("Topical","Twice daily (BID)","7"),
            ["Ointment"] = ("Topical","Once daily","7"), ["Drops"] = ("Ophthalmic","Four times daily (QID)","5"),
            ["Inhaler"] = ("Inhaled","Twice daily (BID)","30"), ["Patch"] = ("Topical","Once daily","7"),
            ["Suppository"] = ("Rectal","Once daily","3"), ["Solution"] = ("Oral","Three times daily (TID)","5"),
        };
        #endregion

        #region Visit Lifecycle
        public async Task BeginVisitAsync(
            int visitId, int patientId, string patientName, string authToken,
            int patientAge = 0, string patientSex = "", string? patientBloodGroup = null)
        {
            _patientId         = patientId;
            _patientName       = patientName;
            _authToken         = authToken;
            _patientAge        = patientAge;
            _patientSex        = patientSex;
            _patientBloodGroup = patientBloodGroup;
            _visitStartTime    = DateTime.Now;
            _currentVisitId    = visitId;
            OnPropertyChanged(nameof(CurrentVisitId));
            OnPropertyChanged(nameof(HasActiveVisit));
            OnPropertyChanged(nameof(CanSaveVisit));
            VisitHeaderText = $"Visit - {patientName}";
            StatusMessage   = $"Visit #{visitId} active";
            await Task.WhenAll(LoadTestCatalogAsync(), LoadDrugCatalogAsync());
        }

        public async Task SaveVisitAsync()
        {
            if (_currentVisitId == 0) { ShowError("No active visit to save."); return; }
            if (string.IsNullOrWhiteSpace(Diagnosis)) { ShowError("Diagnosis is required."); return; }
            try
            {
                StatusMessage = "Saving visit...";
                var result = await _visitService.SaveVisitAsync(BuildSaveRequest(VisitSaveType.Edit));
                if (!result.Success) throw new InvalidOperationException(result.Message);
                await _visitService.EndVisitAsync(_currentVisitId);
                _lastSavedTime = DateTime.Now;
                OnPropertyChanged(nameof(LastSavedText));
                StatusMessage = $"Visit #{_currentVisitId} saved successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving: {ex.Message}";
                _logger.LogError(ex, "Error saving visit {VisitId}", _currentVisitId);
                throw;
            }
        }

        public async Task CompleteVisitAsync()
        {
            if (_currentVisitId == 0) { ShowError("No active visit to complete."); return; }
            if (string.IsNullOrWhiteSpace(Diagnosis)) { ShowError("Diagnosis is required before completing a visit."); return; }
            try
            {
                StatusMessage = "Completing visit...";
                var result = await _visitService.SaveVisitAsync(BuildSaveRequest(VisitSaveType.Edit));
                if (!result.Success) throw new InvalidOperationException(result.Message);
                await _visitService.EndVisitAsync(_currentVisitId);
                StatusMessage = $"Visit #{_currentVisitId} completed";
                _logger.LogInformation("Visit {VisitId} completed", _currentVisitId);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error completing: {ex.Message}";
                _logger.LogError(ex, "Error completing visit {VisitId}", _currentVisitId);
                throw;
            }
        }

        public async Task PauseVisitAsync()
        {
            if (_currentVisitId == 0) { ShowError("No active visit to pause."); return; }
            try
            {
                StatusMessage = "Pausing visit...";
                await _visitService.PauseVisitAsync(_currentVisitId);
                StatusMessage = $"Visit #{_currentVisitId} paused";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error pausing: {ex.Message}";
                _logger.LogError(ex, "Error pausing visit {VisitId}", _currentVisitId);
                throw;
            }
        }

        public int GetCurrentVisitId() => _currentVisitId;

        public void ClearVisitForm()
        {
            Diagnosis = Notes = PresentingSymptoms = HistoryAndExamination =
            ImagingFindings = AiSuggestions = string.Empty;
            ImagingAttachments = new(); HistoryAttachments = new();
            Temperature = 0; BPSystolic = 0; BPDiastolic = 0;
            Gravida = 0; Para = 0; Abortion = 0; LMPDate = null;
            LabResults = new(); Prescriptions = new();
            ClearLabInputs(); ClearRxInputs();
            _currentVisitId = 0; _lastSavedTime = null;
            OnPropertyChanged(nameof(CurrentVisitId));
            OnPropertyChanged(nameof(HasActiveVisit));
            OnPropertyChanged(nameof(CanSaveVisit));
            StatusMessage = "Ready";
        }
        #endregion

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
                Unit        = string.IsNullOrWhiteSpace(LabResultUnit)  ? (matched?.TestUnit    ?? string.Empty) : LabResultUnit.Trim(),
                NormalRange = string.IsNullOrWhiteSpace(LabNormalRange) ? (matched?.NormalRange ?? string.Empty) : LabNormalRange.Trim(),
                Notes       = LabResultNotes.Trim(),
                UnitOptions = matched?.UnitOptions ?? new()
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

        public void AddAttachmentToLabResult(LabResultLineItem item, string filePath)
        {
            if (!System.IO.File.Exists(filePath)) return;
            var att = LabAttachment.FromFile(filePath);
            item.AddAttachment(att);
            LabResults = new List<LabResultLineItem>(LabResults);
            StatusMessage = $"Attached: {att.FileName}";
        }

        public void RemoveAttachmentFromLabResult(LabResultLineItem item, LabAttachment att)
        {
            item.RemoveAttachment(att);
            LabResults = new List<LabResultLineItem>(LabResults);
        }

        public void ClearLabInputs()
        {
            LabTestSearchText = LabResultValue = LabResultUnit = LabResultNotes = LabNormalRange = string.Empty;
            LabSelectedTestId = 0;
            LabUnitOptions = new();
            LabSelectedUnitOption = null;
        }

        public async Task AddNewLabTestToCatalogAsync(string testName, string unitSI, string rangeSI, string unitImp, string rangeImp)
        {
            if (string.IsNullOrWhiteSpace(testName)) return;
            try
            {
                StatusMessage = "Saving new test to catalog...";
                var catalog = CatalogApiHelper.CreateHelper(_settings.ApiBaseUrl, _authToken);
                await catalog.PostTestAsync(new TestCatalogCreateDto
                {
                    TestName            = testName.Trim(),
                    TestUnit            = unitSI.Trim(),
                    NormalRange         = rangeSI.Trim(),
                    UnitImperial        = string.IsNullOrWhiteSpace(unitImp) ? null : unitImp.Trim(),
                    NormalRangeImperial = string.IsNullOrWhiteSpace(rangeImp) ? null : rangeImp.Trim()
                });
                LabTestSearchText = testName.Trim();
                if (!string.IsNullOrWhiteSpace(unitSI)) LabResultUnit = unitSI;
                else if (!string.IsNullOrWhiteSpace(unitImp)) LabResultUnit = unitImp;
                StatusMessage = $"'{testName}' added to catalog";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add test"); ShowError($"Could not save test:\n{ex.Message}");
            }
        }
        #endregion

        #region Prescription Methods
        public void AddPrescription()
        {
            if (!CanAddPrescription) return;
            var matched = _availableDrugs.FirstOrDefault(
                d => string.Equals(d.BrandName, RxDrugText.Trim(), StringComparison.OrdinalIgnoreCase));
            var entry = new PrescriptionLineItem
            {
                DrugId = matched?.DrugId ?? 0, DrugName = matched?.BrandName ?? RxDrugText.Trim(),
                Form = matched?.Form, Dose = RxDose.Trim(), Route = RxRoute, Frequency = RxFrequency,
                DurationDays = RxDuration.Trim(), Instructions = RxInstructions.Trim()
            };
            _lastUsedRx[entry.DrugName] = entry;
            Prescriptions = new List<PrescriptionLineItem>(Prescriptions) { entry };
            ClearRxInputs();
            StatusMessage = "Prescription added";
        }

        public void RemovePrescription(PrescriptionLineItem item)
        {
            Prescriptions = Prescriptions.Where(x => x != item).ToList();
            StatusMessage = "Prescription removed";
        }

        public void AutoFillFromDrug(DrugCatalogDto drug, bool overwrite = false)
        {
            RxDrugText = drug.BrandName;
            _lastUsedRx.TryGetValue(drug.BrandName, out var last);
            FormDefaults.TryGetValue(drug.Form ?? string.Empty, out var fd);
            string? Best(string? a, string? b, string? c) =>
                !string.IsNullOrWhiteSpace(a) ? a : !string.IsNullOrWhiteSpace(b) ? b : c;
            var dose = Best(last?.Dose, drug.DosageStrength, null);
            var route = Best(last?.Route, drug.Route, fd.Route);
            var freq = Best(last?.Frequency, drug.Frequency, fd.Frequency);
            var dur = Best(last?.DurationDays, null, fd.Duration);
            var inst = Best(last?.Instructions, drug.Instructions, null);
            if (overwrite || string.IsNullOrWhiteSpace(RxDose))         RxDose         = dose  ?? string.Empty;
            if (overwrite || string.IsNullOrWhiteSpace(RxRoute))        RxRoute        = route ?? string.Empty;
            if (overwrite || string.IsNullOrWhiteSpace(RxFrequency))    RxFrequency    = freq  ?? string.Empty;
            if (overwrite || string.IsNullOrWhiteSpace(RxDuration))     RxDuration     = dur   ?? string.Empty;
            if (overwrite || string.IsNullOrWhiteSpace(RxInstructions)) RxInstructions = inst  ?? string.Empty;
            StatusMessage = last != null ? $"Auto-filled from last use of {drug.BrandName}"
                : !string.IsNullOrWhiteSpace(route) || !string.IsNullOrWhiteSpace(dose)
                    ? $"Auto-filled defaults for {drug.BrandName}" : $"Selected: {drug.BrandName}";
        }

        public async Task AddNewDrugAndPrescribeAsync(string brandName, string? form, string? strength,
            string? route = null, string? frequency = null, string? instructions = null)
        {
            if (string.IsNullOrWhiteSpace(brandName)) return;
            try
            {
                StatusMessage = "Saving new drug to catalog...";
                var catalog = CatalogApiHelper.CreateHelper(_settings.ApiBaseUrl, _authToken);
                await catalog.PostDrugAsync(new DrugCreateDto
                {
                    BrandName      = brandName.Trim(),
                    Form           = form?.Trim(),
                    DosageStrength = strength?.Trim()
                });
                AvailableDrugs = await _userService.GetDrugCatalogAsync(_settings.ApiBaseUrl, _authToken);
                var newDrug = AvailableDrugs.FirstOrDefault(
                    d => string.Equals(d.BrandName, brandName.Trim(), StringComparison.OrdinalIgnoreCase));
                RxDrugText = newDrug?.BrandName ?? brandName.Trim();
                if (newDrug != null) AutoFillFromDrug(newDrug, overwrite: true);
                else if (!string.IsNullOrWhiteSpace(strength)) RxDose = strength;
                StatusMessage = $"'{brandName}' added to catalog - fill dose details and click Add";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save new drug"); ShowError($"Could not save drug:\n{ex.Message}");
            }
        }

        public void ClearRxInputs()
        {
            RxDrugText = RxDose = RxRoute = RxFrequency = RxDuration = RxInstructions = string.Empty;
        }
        #endregion

        #region Attachment Methods
        public void AddSectionAttachment(string section, string filePath)
        {
            if (!System.IO.File.Exists(filePath)) return;
            var att = LabAttachment.FromFile(filePath);
            if (section == "imaging") { ImagingAttachments = new List<LabAttachment>(_imagingAttachments) { att }; }
            else { HistoryAttachments = new List<LabAttachment>(_historyAttachments) { att }; }
            StatusMessage = $"Attached: {att.FileName}";
        }
        public void RemoveSectionAttachment(string section, LabAttachment att)
        {
            if (section == "imaging") ImagingAttachments = _imagingAttachments.Where(a => a != att).ToList();
            else HistoryAttachments = _historyAttachments.Where(a => a != att).ToList();
        }
        #endregion

        #region Clinical Context (AI)
        public string BuildClinicalContext(string patientName, int patientAge, string patientSex, string? bloodGroup = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== PATIENT CLINICAL CONTEXT ===\n");
            sb.AppendLine($"Patient: {patientName}");
            sb.AppendLine($"Age: {patientAge} years | Sex: {patientSex}");
            if (!string.IsNullOrWhiteSpace(bloodGroup)) sb.AppendLine($"Blood Group: {bloodGroup}");
            if (Temperature > 0) sb.AppendLine($"Temperature: {Temperature}C");
            if (BPSystolic > 0)  sb.AppendLine($"BP: {BPSystolic}/{BPDiastolic} mmHg");
            if (Gravida > 0)     sb.AppendLine($"Obstetric: G{Gravida}P{Para}A{Abortion}");
            if (LMPDate.HasValue) sb.AppendLine($"LMP: {LMPDate:dd MMM yyyy}  |  {EDDText}");
            if (!string.IsNullOrWhiteSpace(PresentingSymptoms)) sb.AppendLine($"\nPresenting Symptoms:\n{PresentingSymptoms}");
            if (!string.IsNullOrWhiteSpace(HistoryAndExamination)) sb.AppendLine($"\nHistory & Examination:\n{HistoryAndExamination}");
            if (!string.IsNullOrWhiteSpace(ImagingFindings)) sb.AppendLine($"\nImaging Findings:\n{ImagingFindings}");
            if (!string.IsNullOrWhiteSpace(Notes)) sb.AppendLine($"\nNotes:\n{Notes}");
            if (LabResults.Count > 0)
            {
                sb.AppendLine("\nLab Results:");
                foreach (var lab in LabResults)
                    sb.AppendLine($"  {lab.TestName}: {lab.ResultValue} {lab.Unit}  (ref: {lab.NormalRange})".TrimEnd());
            }
            if (!string.IsNullOrWhiteSpace(Diagnosis)) sb.AppendLine($"\nWorking Diagnosis:\n{Diagnosis}");
            return sb.ToString();
        }

        private async Task RunAiSuggestAsync()
        {
            if (IsAiThinking || !HasActiveVisit) return;
            const string SystemPrompt =
                "You are an expert clinical decision support assistant. " +
                "Given the patient data below, provide a concise differential diagnosis.\n" +
                "1. Most likely diagnosis with brief reasoning\n2-4. Other differentials\n" +
                "Red flags:\nNext steps:\nKeep it clinically practical.";
            var ctx = BuildClinicalContext(_patientName, _patientAge, _patientSex, _patientBloodGroup);
            IsAiThinking = true;
            AiSuggestions = string.Empty;
            try
            {
                var result = await AiHelper.CompleteAsync(_ai, ctx, SystemPrompt);
                AiSuggestions = string.IsNullOrWhiteSpace(result)
                    ? "No response from AI provider. Check Settings."
                    : result.Trim();
            }
            catch (Exception ex)
            {
                AiSuggestions = $"Error: {ex.Message}";
                _logger.LogError(ex, "AI suggest failed for visit {VisitId}", _currentVisitId);
            }
            finally
            {
                IsAiThinking = false;
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }

        private void ToggleDictation()
        {
            if (_dictation.IsListening) { _dictation.Stop(); IsDictating = false; }
            else
            {
                IsDictating = true;
                _ = _dictation.StartAsync(text => { Notes += text; });
            }
        }
        #endregion

        #region Private Helpers
        private VisitSaveRequest BuildSaveRequest(VisitSaveType saveType) => new()
        {
            VisitId               = _currentVisitId,
            PatientId             = _patientId,
            Diagnosis             = Diagnosis,
            Notes                 = Notes,
            Temperature           = Temperature,
            BloodPressureSystolic  = BPSystolic,
            BloodPressureDiastolic = BPDiastolic,
            Gravida    = Gravida, Para = Para, Abortion = Abortion, LMPDate = LMPDate,
            SaveType   = saveType,
            AuthToken  = _authToken,
            LabResults = LabResults.Select(l => new LabResultCreateDto
            {
                TestId      = l.TestId,
                VisitId     = _currentVisitId,
                ResultValue = l.ResultValue,
                Unit        = l.Unit,
                NormalRange = l.NormalRange,
                Notes       = l.Notes
            }).ToList(),
            Prescriptions = Prescriptions.Select(p => new PrescriptionCreateDto
            {
                DrugId       = p.DrugId,
                VisitId      = _currentVisitId,
                Dosage       = p.Dose,
                Route        = p.Route,
                Frequency    = p.Frequency,
                DurationDays = p.DurationDays,
                Instructions = p.Instructions
            }).ToList()
        };



        private async Task LoadTestCatalogAsync()
        {
            try { AvailableTests = await _userService.GetTestCatalogAsync(_settings.ApiBaseUrl, _authToken); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Test catalog load failed; using mock");
                AvailableTests = new List<TestCatalogDto>
                {
                    new() { TestId=1, TestName="Complete Blood Count", TestUnit="cells/uL", NormalRange="4.5-11.0" },
                    new() { TestId=2, TestName="Glucose Fasting",      TestUnit="mg/dL",    NormalRange="70-100"   }
                };
            }
        }

        private async Task LoadDrugCatalogAsync()
        {
            try { AvailableDrugs = await _userService.GetDrugCatalogAsync(_settings.ApiBaseUrl, _authToken); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Drug catalog load failed; using mock");
                AvailableDrugs = new List<DrugCatalogDto>
                {
                    new() { DrugId=1, BrandName="Paracetamol", Form="Tablet",  DosageStrength="500mg" },
                    new() { DrugId=2, BrandName="Amoxicillin", Form="Capsule", DosageStrength="250mg" }
                };
            }
        }

        private void ShowError(string message, string title = "Error") =>
            OnShowError?.Invoke(title, message);
        #endregion
    }
}
