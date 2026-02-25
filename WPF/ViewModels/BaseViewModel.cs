using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPF.ViewModels
{
    /// <summary>
    /// Base class for all ViewModels. Provides INotifyPropertyChanged and a
    /// SetProperty helper that only fires when the value actually changes.
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Sets <paramref name="field"/> to <paramref name="value"/> and raises
        /// PropertyChanged only when the value differs. Returns true if changed.
        /// </summary>
        protected bool SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
