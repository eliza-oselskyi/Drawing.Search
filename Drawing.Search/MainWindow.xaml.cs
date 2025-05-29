using System;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Drawing.Search.Core;
using Tekla.Structures.Drawing;
using Events = Tekla.Structures.Drawing.UI.Events;
using System.Windows.Media;

namespace Drawing.Search
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new SearchViewModel();

            Loaded += (s, e) => SearchTextBox.Focus();

            if (DataContext is SearchViewModel vm)
            {
                vm.SearchCompleted += (s, e) => SearchTextBox.Focus();
            }
        }

        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Focus();
        }

        private void ThemeToggle_CheckedChanged(object sender, RoutedEventArgs e)
        {
            var resources = Application.Current.Resources;
    
            if (ThemeToggle.IsChecked == true)
            {
                // Switch to Dark Theme
                resources["BackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
                resources["ForegroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                resources["SecondaryBackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D2D"));
                resources["BorderBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#404040"));
                resources["DisabledForegroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999"));
            }
            else
            {
                // Switch to Light Theme
                resources["BackgroundBrush"] = new SolidColorBrush(Colors.White);
                resources["ForegroundBrush"] = new SolidColorBrush(Colors.Black);
                resources["SecondaryBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(204, 204, 204));
                resources["DisabledForegroundBrush"] = new SolidColorBrush(Color.FromRgb(102, 102, 102));
            }
        }
    }
}