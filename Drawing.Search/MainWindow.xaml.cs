using System;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Drawing.Search.Core;
using Tekla.Structures.Drawing.UI;

namespace Drawing.Search
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        
        private readonly Events _events = new Events();
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(GainKeyboardFocus);
            _events.DrawingEditorClosed += AppExit;
            _events.Register();
        }

        private static void AppExit()
        {
            Environment.Exit(0);
        }

        private void GainKeyboardFocus(object sender, RoutedEventArgs e)
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