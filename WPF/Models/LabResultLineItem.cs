using System.Collections.Generic;
using System.Linq;

namespace WPF.ViewModels
{
    /// <summary>One row in the visit lab-results list.</summary>
    public sealed class LabResultLineItem : BaseViewModel
    {
        private List<LabAttachment> _attachments = new();
        private string _resultValue = string.Empty;
        private string _unit        = string.Empty;
        private string _normalRange = string.Empty;
        private List<Core.DTOs.LabUnitOption> _unitOptions = new();
        private string _notes       = string.Empty;

        public int    TestId   { get; set; }
        public string TestName { get; set; } = string.Empty;

        public string ResultValue
        {
            get => _resultValue;
            set => SetProperty(ref _resultValue, value);
        }
        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }
        public string NormalRange
        {
            get => _normalRange;
            set => SetProperty(ref _normalRange, value);
        }
        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        /// <summary>Unit options populated from catalog (SI + imperial). Editable if test not in catalog.</summary>
        public List<Core.DTOs.LabUnitOption> UnitOptions
        {
            get => _unitOptions;
            set { SetProperty(ref _unitOptions, value); OnPropertyChanged(nameof(HasUnitOptions)); }
        }
        public bool HasUnitOptions => _unitOptions.Count > 1;

        public void SelectUnitOption(Core.DTOs.LabUnitOption opt)
        {
            Unit        = opt.Unit;
            NormalRange = opt.NormalRange;
        }

        public List<LabAttachment> Attachments
        {
            get => _attachments;
            set { SetProperty(ref _attachments, value); OnPropertyChanged(nameof(HasAttachments)); }
        }
        public bool HasAttachments => _attachments.Count > 0;

        public void AddAttachment(LabAttachment a)
        {
            _attachments = new List<LabAttachment>(_attachments) { a };
            OnPropertyChanged(nameof(Attachments));
            OnPropertyChanged(nameof(HasAttachments));
        }
        public void RemoveAttachment(LabAttachment a)
        {
            _attachments = _attachments.Where(x => x != a).ToList();
            OnPropertyChanged(nameof(Attachments));
            OnPropertyChanged(nameof(HasAttachments));
        }
    }
}
