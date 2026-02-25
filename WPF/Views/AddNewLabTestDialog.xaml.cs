using System.Windows;

namespace WPF.Views
{
    public partial class AddNewLabTestDialog : Window
    {
        public string TestName  { get; private set; } = string.Empty;
        public string UnitSI    { get; private set; } = string.Empty;
        public string RangeSI   { get; private set; } = string.Empty;
        public string UnitImp   { get; private set; } = string.Empty;
        public string RangeImp  { get; private set; } = string.Empty;

        public AddNewLabTestDialog(string prefillName = "")
        {
            InitializeComponent();
            if (!string.IsNullOrWhiteSpace(prefillName))
                TxtTestName.Text = prefillName;
            Loaded += (_, _) => { TxtTestName.Focus(); TxtTestName.SelectAll(); };
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var name = TxtTestName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Test name is required.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtTestName.Focus();
                return;
            }
            TestName = name;
            UnitSI   = TxtUnitSI.Text.Trim();
            RangeSI  = TxtRangeSI.Text.Trim();
            UnitImp  = TxtUnitImp.Text.Trim();
            RangeImp = TxtRangeImp.Text.Trim();
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
            => DialogResult = false;
    }
}
