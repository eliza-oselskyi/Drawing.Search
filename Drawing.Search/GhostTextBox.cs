using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Drawing.Search
{
    public class GhostTextBox : TextBox
    {
        private TextBlock _ghostTextBlock;
    
        static GhostTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GhostTextBox),
                new FrameworkPropertyMetadata(typeof(GhostTextBox)));
        }

        public GhostTextBox()
        {
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_ghostTextBlock != null) return;

            var contentHost = Template.FindName("PART_ContentHost", this) as ScrollViewer;
            if (contentHost?.Parent is Grid grid)
            {
                _ghostTextBlock = new TextBlock
                {
                    Foreground = Brushes.Gray,
                    Opacity = 0.5,
                    IsHitTestVisible = false,
                    Margin = new Thickness(4, 0, 4, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left
                };

                grid.Children.Add(_ghostTextBlock);
                TextChanged += OnTextChanged;
                UpdateGhostText();
            }
        }

        public static readonly DependencyProperty GhostTextProperty =
            DependencyProperty.Register(
                nameof(GhostText),
                typeof(string),
                typeof(GhostTextBox),
                new PropertyMetadata(string.Empty, OnGhostTextChanged));

        public string GhostText
        {
            get => (string)GetValue(GhostTextProperty);
            set => SetValue(GhostTextProperty, value);
        }

        private static void OnGhostTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GhostTextBox ghostTextBox)
            {
                ghostTextBox.UpdateGhostText();
            }
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateGhostText();
        }

        private void UpdateGhostText()
        {
            if (_ghostTextBlock == null) return;

            if (string.IsNullOrEmpty(Text) || string.IsNullOrEmpty(GhostText))
            {
                _ghostTextBlock.Text = "";
                return;
            }

            if (GhostText.StartsWith(Text, StringComparison.OrdinalIgnoreCase))
            {
                //_ghostTextBlock.Text = GhostText.Substring(Text.Length);
                // TODO: Currently a workaround for the issue with the TextBlock. Idea is to get it to show only the remainder of the suggestion.
                _ghostTextBlock.Text = GhostText;
                // _ghostTextBlock.Text = LeftPad(GhostText.Substring(Text.Length)
                //                                    .Length -
                //                                Text.Length) +
                //                        GhostText.Substring(Text.Length);
            }
            else
            {
                _ghostTextBlock.Text = "";
            }
        }

        private static string LeftPad(int length)
        {
            var s = new StringBuilder();
            for (int i = 0; i < Math.Abs(length); i++)
            {
                s.Append(" ");
            }
            return s.ToString();
        }

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

    public static class VisualTreeHelperExtensions
    {
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