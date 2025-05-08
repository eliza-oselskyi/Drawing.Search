using System.Windows;
using Drawing.Search.Core;

namespace Drawing.Search
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void SelectButton_OnClick(object sender, RoutedEventArgs e)
        {
            var searchManager = new SearchManager();
            var text = SelectTextBox.Text;
            
            searchManager.ExecuteSearch(text);
        }
    }
}