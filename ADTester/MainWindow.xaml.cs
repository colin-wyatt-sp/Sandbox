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
using ADTester.Interfaces;
using ADTester.Model.Data;

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

            //TODO: rem
            passwordBox.Password = "over boord 1!";
        }

        private void initializeLogger() {

            _currentDateTime = DateTime.Now;
            _timeStamp = _currentDateTime.ToString("yyyyMMddHHmmss");
            Logger.Timestamp = _timeStamp;
        }

        private void loggerMessageLogged(LogItem logItem)
        {
            _viewModel.LogItems.Add(logItem);
            Dispatcher.BeginInvoke(new Action(() => { scrollViewer.ScrollToEnd(); }));
            _viewModel.OutputText = logItem.ActionResult.Output;
        }

        private void ToggleAllCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            //TODO: togle arbitary actions list
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (actionListBox.SelectedItem != null)
            {
                _viewModel.Code = ((IArbitraryAction) actionListBox.SelectedItem).Code;
            }
        }

        private void RunButton_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (IArbitraryAction arbitraryAction in _viewModel.ActionList)
            {
                if (arbitraryAction.IsEnabled)
                {
                    tryExectueAction(arbitraryAction);
                }
            }
        }

        private void tryExectueAction(IArbitraryAction arbitraryAction)
        {
            ArbitraryActiveDirectoryAction action = arbitraryAction as ArbitraryActiveDirectoryAction;
            if (action == null) return;

            action.setParameters(_viewModel.Domain, 
                _viewModel.DomainNetbios,
                specificServerRadioButton.IsChecked.Value ? specificServerTextBox.Text : string.Empty, 
                userNameTextBox.Text, 
                passwordBox.Password, 
                isSslCheckbox.IsChecked != null && isSslCheckbox.IsChecked.Value);
            var result = action.executeAction();
            Logger.log(result);
        }

    }
}
