using System.Windows;
using System.Windows.Input;

namespace Drawing.Search.UI.Common.Behaviors
{
    public class TextBoxFocusBehavior
    {
        public static readonly DependencyProperty FocusCommandProperty = DependencyProperty.RegisterAttached(
            "FocusCommand", typeof(ICommand), typeof(TextBoxFocusBehavior),
            new PropertyMetadata(null, OnFocusCommandChanged));

        private static void OnFocusCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                if (e.OldValue != null) element.PreviewMouseDown -= Element_PreviewMouseDown;

                if (e.NewValue != null) element.PreviewMouseDown -= Element_PreviewMouseDown;
            }
        }

        private static void Element_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var element = sender as UIElement;
            if (element != null)
            {
                var command = GetFocusCommand(element);
                if (command?.CanExecute(null) == true) command.CanExecute(null);
            }
        }

        public static ICommand GetFocusCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(FocusCommandProperty);
        }

        public static void SetFocusCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(FocusCommandProperty, value);
        }
    }
}