using System.Windows;
using Drawing.Search.Core;

namespace Drawing.Search
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
                Dispatcher.Invoke(() => Application.Current.Shutdown());
            };
            if (DataContext is SearchViewModel vm) vm.SearchCompleted += (s, e) => SearchTextBox.Focus();
        }

        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Focus();
        }
    }
}