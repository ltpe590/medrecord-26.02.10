using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using WPF.ViewModels;

namespace WPF.Views
{
    public partial class PrintVisitSummaryWindow : Window
    {
        // A4 at 96 dpi
        private const double PageW  = 793.7;
        private const double PageH  = 1122.5;
        private const double PageMargin = 56.0;

        public PrintVisitSummaryWindow(
            string patientName, string patientAge, string patientPhone,
            string diagnosis, string notes,
            string vitals,
            IEnumerable<PrescriptionLineItem> prescriptions,
            IEnumerable<LabResultLineItem> labResults,
            string clinicName, string doctorTitle, string clinicPhone)
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                DocViewer.Document = BuildDoc(
                    patientName, patientAge, patientPhone,
                    diagnosis, notes, vitals,
                    prescriptions.ToList(), labResults.ToList(),
                    clinicName, doctorTitle, clinicPhone);
            };
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            var pd = new PrintDialog();
            if (pd.ShowDialog() != true) return;

            // Explicitly request A4 on the PrintTicket so the printer
            // doesn't default to Letter or another size.
            try
            {
                var ticket = pd.PrintTicket;
                ticket.PageMediaSize  = new System.Printing.PageMediaSize(
                    System.Printing.PageMediaSizeName.ISOA4);
                ticket.PageOrientation = System.Printing.PageOrientation.Portrait;
                pd.PrintTicket = ticket;
            }
            catch { /* printer may not support A4 — FixedDocument is already A4-sized */ }

            pd.PrintDocument(DocViewer.Document.DocumentPaginator, "Visit Summary");
        }

        // ── Build FixedDocument ──────────────────────────────────────────────
        private FixedDocument BuildDoc(
            string patientName, string patientAge, string patientPhone,
            string diagnosis, string notes, string vitals,
            List<PrescriptionLineItem> rx, List<LabResultLineItem> labs,
            string clinicName, string doctorTitle, string clinicPhone)
        {
            var fd = new FixedDocument();
            fd.DocumentPaginator.PageSize = new System.Windows.Size(PageW, PageH);

            var page  = new FixedPage { Width = PageW, Height = PageH, Background = Brushes.White };
            var canvas = BuildCanvas(patientName, patientAge, patientPhone,
                                     diagnosis, notes, vitals, rx, labs,
                                     clinicName, doctorTitle, clinicPhone);
            FixedPage.SetLeft(canvas, 0); FixedPage.SetTop(canvas, 0);
            page.Children.Add(canvas);

            var pc = new PageContent();
            ((System.Windows.Markup.IAddChild)pc).AddChild(page);
            fd.Pages.Add(pc);
            return fd;
        }

        private System.Windows.Controls.Canvas BuildCanvas(
            string patientName, string patientAge, string patientPhone,
            string diagnosis, string notes, string vitals,
            List<PrescriptionLineItem> rx, List<LabResultLineItem> labs,
            string clinicName, string doctorTitle, string clinicPhone)
        {
            var c = new System.Windows.Controls.Canvas { Width = PageW, Height = PageH };
            double x = PageMargin, y = PageMargin, w = PageW - PageMargin * 2;

            // Clinic header
            y = T(c, clinicName,   x, y, w, 18, FontWeights.Bold,   Color(26,35,126)); y += 2;
            y = T(c, doctorTitle,  x, y, w, 12, FontWeights.Normal, Color(80,80,80));
            if (!string.IsNullOrWhiteSpace(clinicPhone))
                y = T(c, clinicPhone, x, y, w, 11, FontWeights.Normal, Color(120,120,120));
            TRight(c, "VISIT SUMMARY", PageW - PageMargin - 130, PageMargin, 130, 10, FontWeights.Bold, Color(26,35,126));
            TRight(c, DateTime.Now.ToString("dd MMM yyyy"), PageW - PageMargin - 130, PageMargin + 14, 130, 10, FontWeights.Normal, Color(100,100,100));

            y += 8; y = Line(c, x, y, w); y += 8;

            // Patient row
            y = Label(c, "PATIENT", x, y);
            y = T(c, patientName, x, y, w * 0.5, 15, FontWeights.Bold, Brushes.Black.Color); y += 2;
            Label(c, "AGE / PHONE", x + w * 0.55, y - 18);
            T(c, $"{patientAge}   {patientPhone}", x + w * 0.55, y - 4, w * 0.45, 12, FontWeights.Normal, Color(60,60,60));

            if (!string.IsNullOrWhiteSpace(vitals))
            {
                y += 6; y = Label(c, "VITALS", x, y);
                y = T(c, vitals, x, y, w, 12, FontWeights.Normal, Color(40,40,40));
            }

            y += 8; y = Line(c, x, y, w); y += 8;

            // Diagnosis
            y = Label(c, "DIAGNOSIS", x, y);
            y = T(c, string.IsNullOrWhiteSpace(diagnosis) ? "—" : diagnosis,
                  x, y, w, 13, FontWeights.Normal, Color(20,20,20)); y += 6;

            // Notes
            if (!string.IsNullOrWhiteSpace(notes))
            {
                y = Label(c, "NOTES", x, y);
                y = T(c, notes, x, y, w, 12, FontWeights.Normal, Color(60,60,60)); y += 6;
            }

            // Prescriptions
            if (rx.Count > 0)
            {
                y = Line(c, x, y, w); y += 6;
                y = T(c, "℞  Medications", x, y, w, 14, FontWeights.Bold, Color(106,27,154)); y += 4;
                int n = 1;
                foreach (var r in rx)
                {
                    y = T(c, $"{n++}.  {r.DrugName}  {r.Form}", x + 8, y, w - 8, 13, FontWeights.SemiBold, Color(26,35,126));
                    string detail = $"     {r.Dose}  ·  {r.Route}  ·  {r.Frequency}  ·  {r.DurationDays} days";
                    y = T(c, detail, x + 8, y, w - 8, 11, FontWeights.Normal, Color(80,80,80));
                    if (!string.IsNullOrWhiteSpace(r.Instructions))
                        y = T(c, $"     ℹ {r.Instructions}", x + 8, y, w - 8, 10, FontWeights.Normal, Color(130,130,130), italic: true);
                    y += 2;
                }
            }

            // Lab results
            if (labs.Count > 0)
            {
                y += 4; y = Line(c, x, y, w); y += 6;
                y = T(c, "🧪  Lab Results", x, y, w, 14, FontWeights.Bold, Color(0,96,100)); y += 4;
                foreach (var lab in labs)
                {
                    string line1 = $"• {lab.TestName}:  {lab.ResultValue} {lab.Unit}";
                    string line2 = string.IsNullOrWhiteSpace(lab.NormalRange) ? "" : $"   Ref: {lab.NormalRange}";
                    y = T(c, line1, x + 8, y, w - 8, 12, FontWeights.SemiBold, Color(0,77,64));
                    if (!string.IsNullOrWhiteSpace(line2))
                        y = T(c, line2, x + 8, y, w - 8, 10, FontWeights.Normal, Color(100,100,100));
                    if (!string.IsNullOrWhiteSpace(lab.Notes))
                        y = T(c, $"   ℹ {lab.Notes}", x + 8, y, w - 8, 10, FontWeights.Normal, Color(130,130,130), italic: true);
                    y += 2;
                }
            }

            // Footer signature
            double footY = PageH - PageMargin - 50;
            Line(c, x, footY, w); footY += 8;
            T(c, "Physician signature:", x, footY, w * 0.5, 10, FontWeights.Normal, Color(120,120,120));
            Line(c, PageW - PageMargin - 170, footY + 24, 170);

            return c;
        }

        // ── Canvas helpers ───────────────────────────────────────────────────
        private static Color Color(byte r, byte g, byte b)
            => System.Windows.Media.Color.FromRgb(r, g, b);

        private static double T(System.Windows.Controls.Canvas c, string text,
            double x, double y, double maxW, double fs,
            FontWeight fw, Color col, bool italic = false)
        {
            if (string.IsNullOrEmpty(text)) return y;
            var tb = new TextBlock
            {
                Text = text, FontSize = fs, FontWeight = fw,
                Foreground = new SolidColorBrush(col),
                MaxWidth = maxW, TextWrapping = TextWrapping.Wrap,
                FontFamily = new FontFamily("Segoe UI"),
                FontStyle = italic ? FontStyles.Italic : FontStyles.Normal
            };
            tb.Measure(new System.Windows.Size(maxW, double.PositiveInfinity));
            System.Windows.Controls.Canvas.SetLeft(tb, x);
            System.Windows.Controls.Canvas.SetTop(tb, y);
            c.Children.Add(tb);
            return y + tb.DesiredSize.Height + 2;
        }

        private static void TRight(System.Windows.Controls.Canvas c, string text,
            double x, double y, double maxW, double fs, FontWeight fw, Color col)
        {
            var tb = new TextBlock
            {
                Text = text, FontSize = fs, FontWeight = fw,
                Foreground = new SolidColorBrush(col),
                MaxWidth = maxW, TextAlignment = TextAlignment.Right,
                FontFamily = new FontFamily("Segoe UI")
            };
            System.Windows.Controls.Canvas.SetLeft(tb, x);
            System.Windows.Controls.Canvas.SetTop(tb, y);
            c.Children.Add(tb);
        }

        private static double Label(System.Windows.Controls.Canvas c, string text, double x, double y)
        {
            var tb = new TextBlock
            {
                Text = text, FontSize = 9, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color(150,150,150)),
                FontFamily = new FontFamily("Segoe UI")
            };
            System.Windows.Controls.Canvas.SetLeft(tb, x);
            System.Windows.Controls.Canvas.SetTop(tb, y);
            c.Children.Add(tb);
            return y + 13;
        }

        private static double Line(System.Windows.Controls.Canvas c, double x, double y, double w)
        {
            var l = new System.Windows.Shapes.Line
            {
                X1 = x, Y1 = y, X2 = x + w, Y2 = y,
                Stroke = new SolidColorBrush(Color(210,210,210)), StrokeThickness = 1
            };
            c.Children.Add(l);
            return y + 1;
        }
    }
}

