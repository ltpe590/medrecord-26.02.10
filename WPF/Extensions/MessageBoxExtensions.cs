using System.Windows;

namespace WPF.Extensions
{
    public static class MessageBoxExtensions
    {
        public static void ShowError(this object _, string message, string title = "Error")
        {
            Application.Current.Dispatcher.Invoke(() =>
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error));
        }

        public static void ShowSuccess(this object _, string message, string title = "Success")
        {
            Application.Current.Dispatcher.Invoke(() =>
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information));
        }

        public static void ShowWarning(this object _, string message, string title = "Warning")
        {
            Application.Current.Dispatcher.Invoke(() =>
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning));
        }

        public static void ShowInfo(this object _, string message, string title = "Information")
        {
            Application.Current.Dispatcher.Invoke(() =>
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information));
        }

        public static bool ShowConfirmation(this object _, string message, string title = "Confirm")
        {
            bool result = false;
            Application.Current.Dispatcher.Invoke(() =>
            {
                result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question)
                        == MessageBoxResult.Yes;
            });
            return result;
        }
    }
}