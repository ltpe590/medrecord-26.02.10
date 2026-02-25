using System.Windows;

namespace WPF.Views
{
    public partial class AddNewDrugDialog : Window
    {
        public string  BrandName    { get; set; } = string.Empty;
        public string? Form         { get; set; }
        public string? Strength     { get; set; }
        public string? Route        { get; set; }
        public string? Frequency    { get; set; }
        public string? Instructions { get; set; }

        public AddNewDrugDialog()
        {
            InitializeComponent();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            if (!string.IsNullOrWhiteSpace(BrandName))
                TxtBrand.Text = BrandName;
            TxtBrand.Focus();
            TxtBrand.SelectAll();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var name = TxtBrand.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Brand/Generic name is required.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtBrand.Focus();
                return;
            }

            BrandName    = name;
            Form         = NullIfEmpty(CbForm.Text);
            Strength     = NullIfEmpty(TxtStrength.Text);
            Route        = NullIfEmpty(CbRoute.Text);
            Frequency    = NullIfEmpty(CbFrequency.Text);
            Instructions = NullIfEmpty(TxtInstructions.Text);
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
            => DialogResult = false;

        private static string? NullIfEmpty(string? s)
            => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }
}
