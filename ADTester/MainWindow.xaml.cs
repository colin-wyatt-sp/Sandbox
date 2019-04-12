using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ADTester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DateTime _currentDateTime;
        private string _timeStamp;
        private readonly ActiveDirectoryTesterViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new ActiveDirectoryTesterViewModel();
            DataContext = _viewModel;
            Logger.MessageLogged += loggerMessageLogged;
        }

        private void initializeLogger() {

            _currentDateTime = DateTime.Now;
            _timeStamp = _currentDateTime.ToString("yyyyMMddHHmmss");
            Logger.Timestamp = _timeStamp;
        }

        private void loggerMessageLogged(LogItem logItem)
        {
            _viewModel.LogItems.Add(logItem);
            //Dispatcher.BeginInvoke(new Action(() => { scrollViewer.ScrollToEnd(); }));
        }
    }
}
