using Core.DTOs;


using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using WPF.ViewModels;

namespace WPF.Views
{
    public partial class VisitTabControl : UserControl
    {
        private VisitPageViewModel? Vm => DataContext as VisitPageViewModel;

        public double VerticalOffset => MainScroller?.VerticalOffset ?? 0;
        public void ScrollToVerticalOffset(double offset) => MainScroller?.ScrollToVerticalOffset(offset);

        public static readonly DependencyProperty SelectedPatientProperty =
            DependencyProperty.Register(nameof(SelectedPatient), typeof(PatientViewModel),
                typeof(VisitTabControl), new PropertyMetadata(null));
        public PatientViewModel? SelectedPatient
        {
            get => (PatientViewModel?)GetValue(SelectedPatientProperty);
            set => SetValue(SelectedPatientProperty, value);
        }

        public static readonly DependencyProperty HasSelectedPatientProperty =
            DependencyProperty.Register(nameof(HasSelectedPatient), typeof(bool),
                typeof(VisitTabControl), new PropertyMetadata(false));
        public bool HasSelectedPatient
        {
            get => (bool)GetValue(HasSelectedPatientProperty);
            set => SetValue(HasSelectedPatientProperty, value);
        }

        public event RoutedEventHandler? SaveVisitRequested;
        public event RoutedEventHandler? CompleteVisitRequested;
        public event RoutedEventHandler? PauseVisitRequested;

        public VisitTabControl()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private VisitPageViewModel? _subscribedVm;
        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_subscribedVm != null)
                _subscribedVm.OnShowError -= ShowVmError;

            _subscribedVm = e.NewValue as VisitPageViewModel;

            if (_subscribedVm != null)
                _subscribedVm.OnShowError += ShowVmError;
        }

        private void ShowVmError(string title, string message)
            => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);

        private void SaveVisitButton_Click(object sender, RoutedEventArgs e)
        => SaveVisitRequested?.Invoke(this, e);

        private void CompleteVisitButton_Click(object sender, RoutedEventArgs e)
        => CompleteVisitRequested?.Invoke(this, e);

        private void PauseVisitButton_Click(object sender, RoutedEventArgs e)
        => PauseVisitRequested?.Invoke(this, e);

        private void BtnPrintSummary_Click(object sender, RoutedEventArgs e)
        {
            var vm = Vm; var patient = SelectedPatient;
            if (vm == null || patient == null) { MessageBox.Show("No patient selected.", "Print Summary", MessageBoxButton.OK, MessageBoxImage.Information); return; }

            string vitals = string.Join("   ", new[] { vm.Temperature != 0 ? $"Temp: {vm.Temperature}C" : null, vm.BPSystolic != 0 ? $"BP: {vm.BPSystolic}/{vm.BPDiastolic} mmHg" : null }.Where(x => x != null));
            new PrintVisitSummaryWindow(patientName: patient.Name, patientAge: $"{patient.Age} yrs / {patient.SexDisplay}",
                patientPhone: patient.Phone ?? string.Empty, diagnosis: vm.Diagnosis, notes: vm.Notes, vitals: vitals,
                prescriptions: vm.Prescriptions, labResults: vm.LabResults, clinicName: vm.PrintClinicName,
                doctorTitle: vm.PrintDoctorLine, clinicPhone: vm.PrintClinicPhone) { Owner = Window.GetWindow(this) }.ShowDialog();
        }

        private void AddPrescriptionButton_Click(object sender, RoutedEventArgs e) => Vm?.AddPrescription();
        private void RemovePrescriptionButton_Click(object sender, RoutedEventArgs e)
        { if (sender is Button btn && btn.Tag is PrescriptionLineItem entry) Vm?.RemovePrescription(entry); }

        private void TxtRxDrug_TextChanged(object sender, TextChangedEventArgs e)
        {
            var vm = Vm; if (vm == null) return;
            var text = (sender as TextBox)?.Text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text)) { DrugSuggestPopup.IsOpen = false; return; }
            var m = vm.AvailableDrugs.Where(d => d.BrandName.Contains(text, StringComparison.OrdinalIgnoreCase)).Take(8).ToList();
            DrugSuggestList.ItemsSource = m; DrugSuggestPopup.IsOpen = m.Count > 0;
        }
        private void TxtRxDrug_LostFocus(object sender, RoutedEventArgs e)
            => Dispatcher.InvokeAsync(() => DrugSuggestPopup.IsOpen = false, System.Windows.Threading.DispatcherPriority.Background);
        private void DrugSuggestList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DrugSuggestList.SelectedItem is DrugCatalogDto sel && Vm != null)
            { Vm.AutoFillFromDrug(sel, overwrite: true); DrugSuggestPopup.IsOpen = false; TxtRxDrug.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)); }
        }
        private async void BtnAddNewDrug_Click(object sender, RoutedEventArgs e)
        {
            var vm = Vm; if (vm == null) return;
            var dlg = new AddNewDrugDialog { Owner = Window.GetWindow(this) }; dlg.BrandName = vm.RxDrugText;
            if (dlg.ShowDialog() != true) return;
            await vm.AddNewDrugAndPrescribeAsync(dlg.BrandName, dlg.Form, dlg.Strength, dlg.Route, dlg.Frequency, dlg.Instructions);
        }
        private void BtnPrintPrescription_Click(object sender, RoutedEventArgs e)
        {
            var vm = Vm; if (vm == null) return;
            if (!vm.Prescriptions.Any()) { MessageBox.Show("No prescriptions to print.", "Nothing to Print", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            var patient = SelectedPatient;
            new PrintPrescriptionWindow(patientName: patient?.Name ?? "-", patientAge: patient != null ? $"{patient.Age} yrs / {patient.SexDisplay}" : "-",
                diagnosis: vm.Diagnosis, prescriptions: vm.Prescriptions, clinicName: vm.PrintClinicName,
                doctorTitle: vm.PrintDoctorLine, clinicPhone: vm.PrintClinicPhone, doctorName: vm.PrintDoctorLine)
            { Owner = Window.GetWindow(this) }.ShowDialog();
        }

        private void AddLabResultButton_Click(object sender, RoutedEventArgs e) => Vm?.AddLabResult();
        private void RemoveLabResultButton_Click(object sender, RoutedEventArgs e)
        { if (sender is Button btn && btn.Tag is LabResultLineItem item) Vm?.RemoveLabResult(item); }

        private void TxtLabTest_TextChanged(object sender, TextChangedEventArgs e)
        {
            var vm = Vm; if (vm == null) return;
            var text = (sender as TextBox)?.Text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text)) { LabSuggestPopup.IsOpen = false; return; }
            var m = vm.AvailableTests.Where(t => t.TestName.Contains(text, StringComparison.OrdinalIgnoreCase)).Take(8).ToList();
            LabSuggestList.ItemsSource = m; LabSuggestPopup.IsOpen = m.Count > 0;
        }
        private void TxtLabTest_LostFocus(object sender, RoutedEventArgs e)
            => Dispatcher.InvokeAsync(() => LabSuggestPopup.IsOpen = false, System.Windows.Threading.DispatcherPriority.Background);
        private void LabSuggestList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LabSuggestList.SelectedItem is TestCatalogDto sel && Vm != null)
            {
                Vm.LabTestSearchText = sel.TestName; Vm.LabSelectedTestId = sel.TestId;
                Vm.LabUnitOptions = sel.UnitOptions; Vm.LabSelectedUnitOption = sel.UnitOptions.FirstOrDefault();
                if (sel.UnitOptions.Count == 0) Vm.LabResultUnit = sel.TestUnit;
                LabSuggestPopup.IsOpen = false; TxtLabTest.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }
        private void CbLabUnit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        { if (CbLabUnit.SelectedItem is LabUnitOption opt && Vm != null) Vm.LabSelectedUnitOption = opt; }
        private void CbLabNormalRange_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var vm = Vm;
            if (CbLabNormalRange.SelectedItem is not string selected || vm == null || !vm.LabUnitOptions.Any()) return;
            var match = vm.LabUnitOptions.FirstOrDefault(o => { var d = string.IsNullOrWhiteSpace(o.Unit) ? o.NormalRange : $"{o.NormalRange}  ({o.Unit})"; return string.Equals(d, selected, StringComparison.OrdinalIgnoreCase); });
            if (match != null) { CbLabUnit.SelectionChanged -= CbLabUnit_SelectionChanged; vm.LabSelectedUnitOption = match; CbLabUnit.SelectionChanged += CbLabUnit_SelectionChanged; }
        }
        private async void BtnAddNewLabTest_Click(object sender, RoutedEventArgs e)
        {
            var vm = Vm; if (vm == null) return;
            var dlg = new AddNewLabTestDialog(vm.LabTestSearchText) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() != true) return;
            await vm.AddNewLabTestToCatalogAsync(dlg.TestName, dlg.UnitSI, dlg.RangeSI, dlg.UnitImp, dlg.RangeImp);
            var added = vm.AvailableTests.FirstOrDefault(t => t.TestName.Equals(dlg.TestName, StringComparison.OrdinalIgnoreCase));
            if (added != null) { vm.LabUnitOptions = added.UnitOptions; vm.LabSelectedUnitOption = added.UnitOptions.FirstOrDefault(); }
        }
        private void BtnOcrLabImage_Click(object sender, RoutedEventArgs e)
        {
            var vm = Vm; if (vm == null || SelectedPatient == null) return;
            var dlg = new Microsoft.Win32.OpenFileDialog { Title = "Select lab result image", Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.tif|All files|*.*", Multiselect = true };
            if (dlg.ShowDialog(Window.GetWindow(this)) != true) return;
            OcrStatusBadge.Visibility = Visibility.Visible; TxtOcrStatus.Text = "Saving..."; BtnOcrLabImage.IsEnabled = false;
            try
            {
                int added = 0;
                foreach (var file in dlg.FileNames)
                {
                    var entry = new LabResultLineItem { TestId = 0, TestName = "IMG: " + System.IO.Path.GetFileNameWithoutExtension(file), ResultValue = string.Empty, Unit = string.Empty, NormalRange = string.Empty };
                    vm.LabResults = new System.Collections.Generic.List<LabResultLineItem>(vm.LabResults) { entry };
                    vm.AddAttachmentToLabResult(entry, file); added++;
                }
                TxtOcrStatus.Text = added == 1 ? "Saved" : $"{added} saved";
            }
            catch (Exception ex) { TxtOcrStatus.Text = "Error: " + ex.Message; }
            finally
            {
                BtnOcrLabImage.IsEnabled = true;
                _ = System.Threading.Tasks.Task.Delay(3000).ContinueWith(_ => Dispatcher.Invoke(() => OcrStatusBadge.Visibility = Visibility.Collapsed));
            }
        }

        private void VisitTab_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.D && (Keyboard.Modifiers & ModifierKeys.Control) != 0) { e.Handled = true; _ = RunAiSuggestAsync(); }
        }
        private async void BtnAiSuggest_Click(object sender, RoutedEventArgs e) => await RunAiSuggestAsync();

        private void BtnMic_Click(object sender, RoutedEventArgs e)
        {
            var vm = Vm; if (vm == null) return;
            vm.ToggleDictationCommand.Execute(null);
            if (vm.IsDictating) SetMicUiListening();
            else SetMicUiStopped();
        }















        private void SetMicUiListening()
        {
            if (BtnMic == null) return;
            BtnMic.Background = System.Windows.Media.Brushes.Red;
            BtnMic.ToolTip = "Stop dictation";
        }

        private void SetMicUiStopped()
        {
            if (BtnMic == null) return;
            BtnMic.ClearValue(BackgroundProperty);
            BtnMic.ToolTip = "Start dictation";
        }
        private async Task RunAiSuggestAsync()
        {
            var vm = Vm; if (vm == null || vm.IsAiThinking) return;
            AiSuggestionsPanel.Visibility = Visibility.Visible;
            if (vm.AiSuggestCommand is WPF.Commands.AsyncRelayCommand cmd)
                await cmd.ExecuteAsync();
        }










        private void AttachImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not LabResultLineItem item) return;
            var dlg = new Microsoft.Win32.OpenFileDialog { Title = "Attach image or document", Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.gif|PDF|*.pdf|All files|*.*", Multiselect = true };
            if (dlg.ShowDialog(Window.GetWindow(this)) != true) return;
            foreach (var f in dlg.FileNames) Vm?.AddAttachmentToLabResult(item, f);
        }
        private void ScanImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not LabResultLineItem item) return;
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("wfs.exe") { UseShellExecute = true });
                MessageBox.Show("Scan your document in Windows Fax and Scan, save it, then click OK.", "Scan", MessageBoxButton.OK, MessageBoxImage.Information);
                var dlg = new Microsoft.Win32.OpenFileDialog { Title = "Select scanned file", Filter = "Images & PDFs|*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.tif;*.pdf|All files|*.*", InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) };
                if (dlg.ShowDialog(Window.GetWindow(this)) == true) Vm?.AddAttachmentToLabResult(item, dlg.FileName);
            }
            catch
            {
                var dlg = new Microsoft.Win32.OpenFileDialog { Title = "Select image to attach", Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.tif|All files|*.*" };
                if (dlg.ShowDialog(Window.GetWindow(this)) == true) Vm?.AddAttachmentToLabResult(item, dlg.FileName);
            }
        }
        private void AttachmentImage_Click(object sender, MouseButtonEventArgs e)
        {
            string? path = null;
            if (sender is System.Windows.Controls.Image img && img.Tag is string p) path = p;
            else if (sender is System.Windows.Controls.TextBlock tb && tb.Tag is string p2) path = p2;
            if (path != null) OpenFileOrDicom(path);
        }
        private void RemoveAttachmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not LabAttachment attachment) return;
            var parent = btn.Parent as FrameworkElement;
            while (parent != null) { if (parent.DataContext is LabResultLineItem li) { Vm?.RemoveAttachmentFromLabResult(li, attachment); return; } parent = parent.Parent as FrameworkElement; }
        }
        private void SectionAttach_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || Vm == null) return;
            var section = btn.Tag as string ?? "imaging";
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = section == "imaging" ? "Attach imaging / report" : "Attach history document",
                Filter = section == "imaging" ? "Images, Reports & DICOM|*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.tif;*.pdf;*.dcm|All files|*.*" : "Documents & Images|*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.tif;*.pdf;*.doc;*.docx|All files|*.*",
                Multiselect = true
            };
            if (dlg.ShowDialog(Window.GetWindow(this)) != true) return;
            foreach (var file in dlg.FileNames) Vm.AddSectionAttachment(section, file);
        }
        private void SectionAttachmentOpen_Click(object sender, MouseButtonEventArgs e)
        { var path = (sender as FrameworkElement)?.Tag as string; if (!string.IsNullOrWhiteSpace(path) && System.IO.File.Exists(path)) OpenFileOrDicom(path); }
        private void SectionAttachmentRemove_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not LabAttachment att || Vm == null) return;
            if (Vm.ImagingAttachments.Contains(att)) Vm.RemoveSectionAttachment("imaging", att);
            else Vm.RemoveSectionAttachment("history", att);
        }
        private void VisitTab_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            const double scrollAmount = 40;
            double delta = e.Delta > 0 ? -scrollAmount : scrollAmount;
            if (sender is ScrollViewer sv) sv.ScrollToVerticalOffset(sv.VerticalOffset + delta);
            e.Handled = true;
        }
        private void OpenFileOrDicom(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !System.IO.File.Exists(filePath)) return;
            var ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            if (ext == ".dcm" || ext == ".dicom")
                new DicomViewerWindow(filePath) { Owner = Window.GetWindow(this) }.Show();
            else
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
        }
    }
}
