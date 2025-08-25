using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Drawing.Search
{
    /// <summary>
    ///     A custom <see cref="TextBox" /> control that displays ghost text (placeholder text)
    ///     when the text box is empty or partially filled.
    /// </summary>
    public class GhostTextBox : TextBox
    {
        /// <summary>
        ///     Dependency property for the ghost text that acts as placeholder text.
        /// </summary>
        public static readonly DependencyProperty GhostTextProperty =
            DependencyProperty.Register(
                nameof(GhostText),
                typeof(string),
                typeof(GhostTextBox),
                new PropertyMetadata(string.Empty, OnGhostTextChanged));

        public static readonly DependencyProperty GhostTextColorProperty = DependencyProperty.Register(
            nameof(GhostTextColor),
            typeof(Brush),
            typeof(GhostTextBox),
            new FrameworkPropertyMetadata(
                new SolidColorBrush(Color.FromRgb(153, 153, 153)),
                FrameworkPropertyMetadataOptions.AffectsRender,
                OnGhostTextColorChanged));

        private TextBlock _ghostTextBlock;

        /// <summary>
        ///     Static constructor that sets the default style key for the <see cref="GhostTextBox" /> class.
        /// </summary>
        static GhostTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GhostTextBox),
                new FrameworkPropertyMetadata(typeof(GhostTextBox)));
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="GhostTextBox" /> class.
        ///     Sets up the event handler for the control's <see cref="FrameworkElement.Loaded" /> event.
        /// </summary>
        public GhostTextBox()
        {
            Loaded += OnLoaded;
            DefaultStyleKey = typeof(GhostTextBox);
        }

        public Brush GhostTextColor
        {
            get => (Brush)GetValue(GhostTextColorProperty);
            set => SetValue(GhostTextColorProperty, value);
        }

        /// <summary>
        ///     Gets or sets the placeholder (ghost) text displayed in the text box when it is empty.
        /// </summary>
        public string GhostText
        {
            get => (string)GetValue(GhostTextProperty);
            set => SetValue(GhostTextProperty, value);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            Padding = new Thickness(5, 0, 5, 0);
            BorderThickness = new Thickness(1);
        }

        private static void OnGhostTextColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GhostTextBox ghostTextBox && ghostTextBox._ghostTextBlock != null)
                ghostTextBox._ghostTextBlock.Foreground = (Brush)e.NewValue;
        }

        private void UpdateGhostText()
        {
            if (_ghostTextBlock == null) return;

            if (string.IsNullOrEmpty(Text))
            {
                _ghostTextBlock.Text = GhostText;
                _ghostTextBlock.Visibility = Visibility.Visible;
            }
            else if (!string.IsNullOrEmpty(GhostText) && GhostText.StartsWith(Text, StringComparison.OrdinalIgnoreCase))
            {
                _ghostTextBlock.Text = GhostText;
                _ghostTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                _ghostTextBlock.Text = string.Empty;
                _ghostTextBlock.Visibility = Visibility.Collapsed;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_ghostTextBlock != null) return;

            var contentHost = Template.FindName("PART_ContentHost", this) as ScrollViewer;
            if (contentHost?.Parent is Grid grid)
            {
                _ghostTextBlock = new TextBlock
                {
                    Foreground = GhostTextColor,
                    Opacity = 0.5,
                    IsHitTestVisible = false,
                    // Margin should be the same as the padding of the text box, but we need to add the width of the caret
                    Margin = new Thickness(6, 0, 6, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Visibility = string.IsNullOrEmpty(Text) ? Visibility.Visible : Visibility.Collapsed
                };

                grid.Children.Add(_ghostTextBlock);
                TextChanged += OnTextChanged;
                UpdateGhostText();
            }
        }

        /// <summary>
        ///     Called when the <see cref="GhostText" /> property changes. Updates the ghost text display.
        /// </summary>
        /// <param name="d">The dependency object where the property changed.</param>
        /// <param name="e">Details about the property change.</param>
        private static void OnGhostTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GhostTextBox ghostTextBox) ghostTextBox.UpdateGhostText();
        }

        /// <summary>
        ///     Handles the <see cref="TextBox.TextChanged" /> event to update the ghost text
        ///     when the text in the text box changes.
        /// </summary>
        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateGhostText();
        }

        /// <summary>
        ///     Pads a string with spaces to the specified length.
        /// </summary>
        /// <param name="length">The number of spaces to include in the padding.</param>
        /// <returns>A string consisting of the specified number of spaces.</returns>
        private static string LeftPad(int length)
        {
            var s = new StringBuilder();
            for (var i = 0; i < Math.Abs(length); i++) s.Append(" ");
            return s.ToString();
        }

        /// <summary>
        ///     Handles the <see cref="UIElement.PreviewKeyDown" /> event to allow selecting the ghost text
        ///     (via the <see cref="Key.Tab" /> key) as the text for the text box.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (e.Key == Key.Tab && !string.IsNullOrEmpty(_ghostTextBlock?.Text))
            {
                Text = GhostText;
                CaretIndex = Text.Length;
                e.Handled = true;
            }
        }
    }

    /// <summary>
    ///     Extension methods for working with the visual tree in WPF.
    /// </summary>
    public static class VisualTreeHelperExtensions
    {
        /// <summary>
        ///     Safely retrieves the parent of a given <see cref="DependencyObject" />.
        /// </summary>
        /// <param name="element">The element whose parent is to be retrieved.</param>
        /// <returns>The parent <see cref="DependencyObject" />, or <c>null</c> if none exists.</returns>
        public static DependencyObject GetParent(this DependencyObject element)
        {
            try
            {
                return VisualTreeHelper.GetParent(element);
            }
            catch
            {
                return null;
            }
        }
    }
}