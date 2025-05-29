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
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new SearchViewModel();
        }
    }
}