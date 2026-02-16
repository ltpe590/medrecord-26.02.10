using System.Windows;
using System.Windows.Media.Animation;

namespace WPF.Helpers
{
    /// <summary>
    /// Adds fade-in/fade-out animations to any FrameworkElement when its content changes.
    /// Usage: <TextBox helpers:FadeNotificationHelper.Duration="1" Text="{Binding Message}" />
    /// </summary>
    public static class FadeNotificationHelper
    {
        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.RegisterAttached(
                "Duration",
                typeof(double),
                typeof(FadeNotificationHelper),
                new PropertyMetadata(0.0, OnDurationChanged));

        public static readonly DependencyProperty TargetPropertyProperty =
            DependencyProperty.RegisterAttached(
                "TargetProperty",
                typeof(DependencyProperty),
                typeof(FadeNotificationHelper),
                new PropertyMetadata(null));

        public static double GetDuration(DependencyObject obj) => (double)obj.GetValue(DurationProperty);

        public static void SetDuration(DependencyObject obj, double value) => obj.SetValue(DurationProperty, value);

        public static DependencyProperty GetTargetProperty(DependencyObject obj) => (DependencyProperty)obj.GetValue(TargetPropertyProperty);

        public static void SetTargetProperty(DependencyObject obj, DependencyProperty value) => obj.SetValue(TargetPropertyProperty, value);

        private static void OnDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element && e.NewValue is double seconds && seconds > 0)
            {
                // Auto-detect the property to watch (Text for TextBlock/TextBox, Content for ContentControl, etc.)
                var targetProperty = GetTargetProperty(element) ?? DetectDefaultProperty(element);
                if (targetProperty == null) return;

                // Set up binding to watch for changes
                element.SetBinding(targetProperty, new System.Windows.Data.Binding(targetProperty.Name)
                {
                    Source = element,
                    NotifyOnTargetUpdated = true
                });

                element.TargetUpdated += (s, args) => StartFadeAnimation(element, seconds);
            }
        }

        private static DependencyProperty DetectDefaultProperty(FrameworkElement element)
        {
            // Try specific types first
            if (element is System.Windows.Controls.TextBlock)
                return System.Windows.Controls.TextBlock.TextProperty;
            if (element is System.Windows.Controls.TextBox)
                return System.Windows.Controls.TextBox.TextProperty;
            if (element is System.Windows.Controls.Label)
                return System.Windows.Controls.Label.ContentProperty;

            // Fall back to ContentControl for Button, etc.
            if (element is System.Windows.Controls.ContentControl)
                return System.Windows.Controls.ContentControl.ContentProperty;

            throw new NotSupportedException($"No default property detected for element type: {element.GetType().Name}");
        }

        private static void StartFadeAnimation(FrameworkElement element, double seconds)
        {
            // Cancel any existing animation
            element.BeginAnimation(UIElement.OpacityProperty, null);

            // Fade in
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.2))
            {
                FillBehavior = FillBehavior.HoldEnd
            };

            // Fade out
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3))
            {
                BeginTime = TimeSpan.FromSeconds(seconds),
                FillBehavior = FillBehavior.HoldEnd
            };

            // Chain animations
            element.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            element.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }
    }
}