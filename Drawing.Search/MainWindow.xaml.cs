using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
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
            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Keyboard.Focus(SelectTextBox);
        }

        private void SelectButton_OnClick(object sender, RoutedEventArgs e)
        {
            var searchManager = new SearchManager();
            var text = SelectTextBox.Text;
            Keyboard.Focus(SelectTextBox);
            
            if (AssemblyRadio.IsChecked == true)
            {
                Task.Run(() => searchManager.ExecuteSearch(text));
            }
            else if (DetailRadio.IsChecked == true)
            {
                Task.Run(() => searchManager.ExecuteDetailSearch(text));
            }
            else if (PartMarkRadio.IsChecked == true)
            {
                Task.Run(() => searchManager.ExecutePartMarkSearch(text));
            }
        }

        private void SelectTextBox_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SelectButton_OnClick(sender, e);
            }
        }
    }
}