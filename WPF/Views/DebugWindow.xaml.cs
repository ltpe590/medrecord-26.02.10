using System;
using System.Windows;

namespace WPF.Views
{
    public partial class DebugWindow : Window
    {
        public DebugWindow()
        {
            InitializeComponent();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Debug log viewer is temporarily disabled.\nLogs are available in Visual Studio Output window.",
                "Info",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            LogTextBox.Clear();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Use Visual Studio Output → Copy All for now.",
                "Info",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
