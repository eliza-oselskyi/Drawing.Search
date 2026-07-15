using System.Windows;
using Drawing.Search.Infrastructure;
using Drawing.Search.ViewModels;

namespace Drawing.Search.Views
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(SearchViewModel searchViewModel)
        {
            InitializeComponent();
            DataContext = searchViewModel;

            Loaded += (s, e) => SearchTextBox.Focus();
            searchViewModel.QuitRequested += (sender, _) =>
            {
                Dispatcher.Invoke(() => System.Windows.Application.Current.Shutdown());
            };
            if (DataContext is SearchViewModel vm)
            {
                vm.SearchCompleted += (s, e) => SearchTextBox.Focus();
                vm.FocusRequested += (s, e) => SearchTextBox.Focus();
            }
        }

        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Focus();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ClearButton_OnClick(object sender, RoutedEventArgs e)
        {
            var logger = SearchLoggerServiceLocator.Current;

            System.Windows.Application.Current.Dispatcher.Invoke(logger.ClearLog);
        }
    }
}