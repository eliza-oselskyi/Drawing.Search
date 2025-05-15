using System;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Drawing.Search.Core;
using Tekla.Structures.Drawing;
using Events = Tekla.Structures.Drawing.UI.Events;

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
            var dh = new DrawingHandler();
            if (dh.GetActiveDrawing() == null)
            {
                MessageBox.Show("No active drawing open.");
                AppExit();
            }
            else {
                InitializeComponent();
                this.Loaded += new RoutedEventHandler(GainKeyboardFocus);
                _events.DrawingEditorClosed += AppExit;
                _events.Register();
            }
        }

        private static void AppExit()
        {
            Environment.Exit(0);
        }

        private void GainKeyboardFocus(object sender, RoutedEventArgs e)
        {
            Keyboard.Focus(SelectTextBox);
        }

        private void ProgressBarHandle(bool setup)
        {
            if (setup)
            {
                ProgressBar.Visibility = Visibility.Visible;
                ProgressLabel.Visibility = Visibility.Hidden;
            }
            else
            {
                ProgressBar.Visibility = Visibility.Hidden;
                ProgressLabel.Visibility = Visibility.Visible;
            }
        }

        private async Task<int> SearchResults()
        {
            var searchManager = new SearchManager();
            var text = SelectTextBox.Text;
            Keyboard.Focus(SelectTextBox);

            int res;
            if (AssemblyRadio.IsChecked == true)
            {
                res = await Task.Run(() => searchManager.ExecuteAssemblySearch(text));
            }
            else if (DetailRadio.IsChecked == true)
            {
                res = await Task.Run(() => searchManager.ExecuteDetailSearch(text));
            }
            else if (PartMarkRadio.IsChecked == true)
            {
                res = await Task.Run(() => searchManager.ExecutePartMarkSearch(text));
            }
            else
            {
                return -1;
            }

            return res;
        }

        private async void SelectButton_OnClick(object sender, RoutedEventArgs e)
        {
            
            ProgressBarHandle(true);
            var x = await SearchResults();
            ProgressBarHandle(false);
            TextBlock.Text = $@"Results found: {x}";
            Console.WriteLine(x);
        }

        private async void SelectTextBox_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            SelectButton_OnClick(sender, e);
        }
    }
}