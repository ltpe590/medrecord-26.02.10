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

            // Hook up the RequestClose event
            Vm.RequestClose += ok =>
            {
                DialogResult = ok;
                this.BeginAnimation(Window.LeftProperty, null);
                Close();
            };

            this.Left = -400;
            SlideInFromLeft();
        }

        public bool StartVisitImmediately => Vm.StartVisitImmediately;
        public RegisterPatientViewModel Vm { get; }

        public List<string> BloodGroups { get; } = new()
        {
            "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-"
        };

        // Button event handlers
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Vm.SavePatient(false); // Save without starting visit
        }

        private void SaveAndStartVisitButton_Click(object sender, RoutedEventArgs e)
        {
            Vm.SavePatient(true); // Save and start visit
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
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