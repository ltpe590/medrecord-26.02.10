using System.Windows;
using System.Windows.Media.Animation;
using WPF.ViewModels;

namespace WPF.Views
{
    public partial class RegisterPatientWindow : Window
    {
        public RegisterPatientWindow(RegisterPatientViewModel viewModel)
        {
            InitializeComponent();

            Vm = viewModel;
            DataContext = Vm;

            Vm.RequestClose += ok =>
            {
                DialogResult = ok;
                this.BeginAnimation(Window.LeftProperty, null);
                Close();
            };

            Vm.ValidationFailed += msg =>
                System.Windows.MessageBox.Show(msg, "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);

            this.Left = -400;
            SlideInFromLeft();
        }

        public bool StartVisitImmediately => Vm.StartVisitImmediately;
        public RegisterPatientViewModel Vm { get; }

        // BloodGroups moved to RegisterPatientViewModel

        // Guard: prevents double-submit if both buttons are clicked during the close animation
        private bool _saving = false;

        private void DisableSaveButtons()
        {
            // Disable all three action buttons immediately to prevent re-entry
            foreach (var btn in new[] { SaveBtn, SaveAndStartBtn, CancelBtn })
                if (btn != null) btn.IsEnabled = false;
        }

        // Button event handlers
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_saving) return;
            _saving = true;
            DisableSaveButtons();
            Vm.SavePatient(false); // Save without starting visit
        }

        private void SaveAndStartVisitButton_Click(object sender, RoutedEventArgs e)
        {
            if (_saving) return;
            _saving = true;
            DisableSaveButtons();
            Vm.SavePatient(true); // Save and start visit
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_saving) return;
            Vm.Cancel(); // Cancel the operation
        }

        // The animation method
        private void SlideInFromLeft()
        {
            DoubleAnimation slideAnimation = new DoubleAnimation
            {
                From = -400,
                To = 0,
                Duration = new Duration(System.TimeSpan.FromSeconds(0.5)),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
            };

            this.BeginAnimation(Window.LeftProperty, slideAnimation);
        }
    }
}