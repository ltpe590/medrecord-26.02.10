using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
using WPF.ViewModels;

namespace WPF.Views
{
    public partial class PrintPrescriptionWindow : Window
    {
        private readonly ObservableCollection<PrescriptionLineItem> _lines;
        private bool _suppressRefresh;
        public bool Saved { get; private set; }

        // ── A5 page dimensions at 96 dpi ────────────────────────────────────
        private const double PageW = 559.4;   // 148 mm  (A5 width)
        private const double PageH = 793.7;   // 210 mm  (A5 height)
        private const double PageMargin = 42.0;

        public PrintPrescriptionWindow(
            string patientName, string patientAge, string diagnosis,
            IEnumerable<PrescriptionLineItem> prescriptions,
            string clinicName, string doctorTitle, string clinicPhone, string doctorName)
        {
            InitializeComponent();

            _lines = new ObservableCollection<PrescriptionLineItem>(
                prescriptions.Select(p => new PrescriptionLineItem
                {
                    DrugId = p.DrugId, DrugName = p.DrugName, Form = p.Form,
                    Dose = p.Dose, Route = p.Route, Frequency = p.Frequency,
                    DurationDays = p.DurationDays, Instructions = p.Instructions
                }));

            RxEditControl.ItemsSource = _lines;

            _suppressRefresh = true;
            TxtClinicName.Text    = string.IsNullOrWhiteSpace(clinicName)  ? "Clinic Name"  : clinicName;
            TxtDoctorTitle.Text   = string.IsNullOrWhiteSpace(doctorTitle) ? "Physician"    : doctorTitle;
            TxtClinicPhone.Text   = clinicPhone ?? string.Empty;
            TxtPatientName.Text   = patientName;
            TxtPatientAge.Text    = patientAge;
            TxtDiagnosis.Text     = diagnosis;
            TxtSignatureName.Text = string.IsNullOrWhiteSpace(doctorName)  ? doctorTitle    : doctorName;
            TxtFooterNotes.Text   = string.Empty;
            _suppressRefresh = false;

            Loaded += (_, _) => RefreshPreview();
        }

        // ── Triggered by any edit ────────────────────────────────────────────
        private void EditField_Changed(object sender, TextChangedEventArgs e) => RefreshPreview();
        private void RxField_Changed(object sender, TextChangedEventArgs e)   => RefreshPreview();

        private void RemoveRxLine_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PrescriptionLineItem item)
            { _lines.Remove(item); RefreshPreview(); }
        }

        // ── Build FixedDocument and push to DocumentViewer ───────────────────
        private void RefreshPreview()
        {
            if (_suppressRefresh) return;
            try { DocViewer.Document = BuildFixedDocument(); }
            catch { /* layout not ready yet */ }
        }

        private FixedDocument BuildFixedDocument()
        {
            var fd = new FixedDocument();
            fd.DocumentPaginator.PageSize = new System.Windows.Size(PageW, PageH);

            var page = new FixedPage { Width = PageW, Height = PageH, Background = Brushes.White };
            var canvas = BuildPageCanvas();
            FixedPage.SetLeft(canvas, 0);
            FixedPage.SetTop(canvas, 0);
            page.Children.Add(canvas);

            var pc = new PageContent();
            ((System.Windows.Markup.IAddChild)pc).AddChild(page);
            fd.Pages.Add(pc);
            return fd;
        }

        // ── Draw the prescription onto a Canvas ──────────────────────────────
        private Canvas BuildPageCanvas()
        {
            var canvas = new Canvas { Width = PageW, Height = PageH };
            double x = PageMargin, y = PageMargin, w = PageW - PageMargin * 2;

            // ─ Clinic header ─────────────────────────────────────────────────
            y = AddText(canvas, TxtClinicName.Text, x, y, w, 20, FontWeights.Bold,
                        new SolidColorBrush(Color.FromRgb(26, 35, 126)));
            y += 2;
            y = AddText(canvas, TxtDoctorTitle.Text, x, y, w, 13, FontWeights.Normal,
                        new SolidColorBrush(Color.FromRgb(80, 80, 80)));
            if (!string.IsNullOrWhiteSpace(TxtClinicPhone.Text))
                y = AddText(canvas, TxtClinicPhone.Text, x, y, w, 12, FontWeights.Normal,
                            new SolidColorBrush(Color.FromRgb(120, 120, 120)));

            // Date top-right
            AddTextAt(canvas, DateTime.Now.ToString("dd MMM yyyy"),
                      PageW - PageMargin - 120, PageMargin, 120, 12, FontWeights.Normal,
                      new SolidColorBrush(Color.FromRgb(100, 100, 100)));

            y += 10;
            y = DrawLine(canvas, x, y, w); y += 8;

            // ─ Patient row ───────────────────────────────────────────────────
            y = AddLabel(canvas, "PATIENT", x, y);
            y = AddText(canvas, TxtPatientName.Text, x, y, w * 0.55, 15,
                        FontWeights.SemiBold, Brushes.Black);
            AddLabel(canvas, "AGE / SEX", x + w * 0.6, y - 18);
            AddText(canvas, TxtPatientAge.Text, x + w * 0.6, y - 4, w * 0.4, 13,
                    FontWeights.Normal, new SolidColorBrush(Color.FromRgb(60, 60, 60)));
            y += 8;
            y = DrawLine(canvas, x, y, w); y += 8;

            // ─ Diagnosis ─────────────────────────────────────────────────────
            y = AddLabel(canvas, "DIAGNOSIS", x, y);
            y = AddText(canvas, TxtDiagnosis.Text, x, y, w, 13, FontWeights.Normal,
                        new SolidColorBrush(Color.FromRgb(40, 40, 40)));
            y += 8;
            y = DrawLine(canvas, x, y, w); y += 8;

            // ─ Rx heading ────────────────────────────────────────────────────
            y = AddText(canvas, "℞  Medications", x, y, w, 16, FontWeights.Bold,
                        new SolidColorBrush(Color.FromRgb(21, 101, 192)));
            y += 6;

            // ─ Drug lines ────────────────────────────────────────────────────
            int n = 1;
            foreach (var rx in _lines)
            {
                y = AddText(canvas, $"{n++}.  {rx.DrugName}  {rx.Form}", x + 10, y, w - 10,
                            14, FontWeights.Bold,
                            new SolidColorBrush(Color.FromRgb(26, 35, 126)));
                string detail = $"     {rx.Dose}  ·  {rx.Route}  ·  {rx.Frequency}";
                if (!string.IsNullOrWhiteSpace(rx.DurationDays))
                    detail += $"  ·  {rx.DurationDays} days";
                y = AddText(canvas, detail, x + 10, y, w - 10, 12, FontWeights.Normal,
                            new SolidColorBrush(Color.FromRgb(80, 80, 80)));
                if (!string.IsNullOrWhiteSpace(rx.Instructions))
                    y = AddText(canvas, $"     ℹ {rx.Instructions}", x + 10, y, w - 10, 11,
                                FontWeights.Normal,
                                new SolidColorBrush(Color.FromRgb(130, 130, 130)),
                                italic: true);
                y += 4;
            }

            // ─ Footer ────────────────────────────────────────────────────────
            double footerY = PageH - PageMargin - 60;
            DrawLine(canvas, x, footerY, w); footerY += 8;

            if (!string.IsNullOrWhiteSpace(TxtFooterNotes.Text))
                AddText(canvas, $"Notes: {TxtFooterNotes.Text}", x, footerY, w * 0.6, 11,
                        FontWeights.Normal, new SolidColorBrush(Color.FromRgb(100, 100, 100)));

            // Signature line on the right
            DrawLine(canvas, PageW - PageMargin - 160, footerY + 28, 160);
            AddText(canvas, TxtSignatureName.Text,
                    PageW - PageMargin - 160, footerY + 32, 160, 11,
                    FontWeights.Normal, new SolidColorBrush(Color.FromRgb(80, 80, 80)));

            return canvas;
        }

        // ── Canvas drawing helpers ───────────────────────────────────────────

        private static double AddLabel(Canvas c, string text, double x, double y)
        {
            var tb = new TextBlock
            {
                Text = text, FontSize = 9, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                FontFamily = new FontFamily("Segoe UI")
            };
            Canvas.SetLeft(tb, x); Canvas.SetTop(tb, y);
            c.Children.Add(tb);
            return y + 14;
        }

        private static double AddText(Canvas c, string text, double x, double y,
            double maxW, double fontSize, FontWeight weight, Brush brush,
            bool italic = false)
        {
            if (string.IsNullOrEmpty(text)) return y;
            var tb = new TextBlock
            {
                Text = text, FontSize = fontSize, FontWeight = weight,
                Foreground = brush, MaxWidth = maxW,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new FontFamily("Segoe UI"),
                FontStyle = italic ? FontStyles.Italic : FontStyles.Normal
            };
            tb.Measure(new System.Windows.Size(maxW, double.PositiveInfinity));
            Canvas.SetLeft(tb, x); Canvas.SetTop(tb, y);
            c.Children.Add(tb);
            return y + tb.DesiredSize.Height + 2;
        }

        private static void AddTextAt(Canvas c, string text, double x, double y,
            double maxW, double fontSize, FontWeight weight, Brush brush)
        {
            var tb = new TextBlock
            {
                Text = text, FontSize = fontSize, FontWeight = weight,
                Foreground = brush, MaxWidth = maxW, TextAlignment = TextAlignment.Right,
                FontFamily = new FontFamily("Segoe UI")
            };
            Canvas.SetLeft(tb, x); Canvas.SetTop(tb, y);
            c.Children.Add(tb);
        }

        private static double DrawLine(Canvas c, double x, double y, double w)
        {
            var line = new System.Windows.Shapes.Line
            {
                X1 = x, Y1 = y, X2 = x + w, Y2 = y,
                Stroke = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                StrokeThickness = 1
            };
            c.Children.Add(line);
            return y + 1;
        }

        // ── Actions ──────────────────────────────────────────────────────────

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        { DialogResult = false; Close(); }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var pd = new PrintDialog();
                // Show only the printer-selection dialog (not preview — we already show our own)
                if (pd.ShowDialog() != true) return;

                // Force A5 paper on the PrintTicket
                try
                {
                    var ticket = pd.PrintTicket;
                    ticket.PageMediaSize = new System.Printing.PageMediaSize(
                        System.Printing.PageMediaSizeName.ISOA5);
                    ticket.PageOrientation = System.Printing.PageOrientation.Portrait;
                    pd.PrintTicket = ticket;
                }
                catch { /* printer may not support A5 — fall through and print at document size */ }

                var fd = BuildFixedDocument();
                // PrintDocument sends FixedDocument directly — full fidelity, no "unsupported" message
                pd.PrintDocument(fd.DocumentPaginator,
                    $"Prescription – {TxtPatientName.Text}");

                TxtStatus.Text = "✅ Sent to printer.";
                Saved = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Print error:\n{ex.Message}", "Print Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    FileName   = $"Rx_{TxtPatientName.Text.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}",
                    DefaultExt = ".txt",
                    Filter     = "Text file|*.txt|All files|*.*"
                };
                if (dlg.ShowDialog(this) != true) return;
                File.WriteAllText(dlg.FileName, BuildPlainText());
                TxtStatus.Text = $"✅ Saved to {Path.GetFileName(dlg.FileName)}";
                Saved = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Save error:\n{ex.Message}", "Save Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string BuildPlainText()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(TxtClinicName.Text);
            sb.AppendLine(TxtDoctorTitle.Text);
            if (!string.IsNullOrWhiteSpace(TxtClinicPhone.Text)) sb.AppendLine(TxtClinicPhone.Text);
            sb.AppendLine(new string('─', 42));
            sb.AppendLine($"Patient : {TxtPatientName.Text}");
            sb.AppendLine($"Age/Sex : {TxtPatientAge.Text}");
            sb.AppendLine($"Date    : {DateTime.Now:dd MMM yyyy}");
            sb.AppendLine(new string('─', 42));
            sb.AppendLine($"Diagnosis: {TxtDiagnosis.Text}");
            sb.AppendLine(); sb.AppendLine("Rx"); sb.AppendLine(new string('─', 42));
            int n = 1;
            foreach (var rx in _lines)
            {
                sb.AppendLine($"{n++}. {rx.DrugName}  {rx.Form}");
                sb.AppendLine($"   {rx.Dose}  ·  {rx.Route}  ·  {rx.Frequency}  ·  {rx.DurationDays} days");
                if (!string.IsNullOrWhiteSpace(rx.Instructions))
                    sb.AppendLine($"   ℹ {rx.Instructions}");
                sb.AppendLine();
            }
            sb.AppendLine(new string('─', 42));
            if (!string.IsNullOrWhiteSpace(TxtFooterNotes.Text))
                sb.AppendLine($"Notes: {TxtFooterNotes.Text}");
            sb.AppendLine($"Physician: {TxtSignatureName.Text}");
            return sb.ToString();
        }
    }
}

