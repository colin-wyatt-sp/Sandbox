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
            _viewModel.Domain = "siqsus.forest";
            _viewModel.DomainNetbios = "siqsus";
            _viewModel.SpecificServer = string.Empty;
            _viewModel.Username = "Administrator";
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
            //_viewModel.OutputText = logItem.ActionResult.Output;
            _viewModel.SelectedLogItem = logItem;
        }

        private void ToggleAllCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            SelectAll(true);
        }

        private void ToggleAllCheckbox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            SelectAll(false);
        }

        private void SelectAll(bool select)
        {
            if (actionListBox == null) return;
            var all = actionListBox.ItemsSource as IEnumerable<IArbitraryAction>;
            if (all != null)
            {
                foreach (var source in all)
                    source.IsEnabled = select;
            }
        }

        private void actionListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (actionListBox.SelectedItem != null)
            {
                //_viewModel.Code = ((IArbitraryAction) actionListBox.SelectedItem).Code;
                _viewModel.SelectedAction = (IArbitraryAction)actionListBox.SelectedItem;
                //txtCode.Load();
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

            if (result != null) {
                Logger.log(result);
            }
        }

        private void logItemsListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (logItemsListBox.SelectedItem != null)
            {
                //_viewModel.OutputText = ((LogItem) logItemsListBox.SelectedItem).ActionResult.Output;
                _viewModel.SelectedLogItem = ((LogItem)logItemsListBox.SelectedItem);
            }
        }



        private void ClearButton_OnClick(object sender, RoutedEventArgs e)
        {
            _viewModel.LogItems.Clear();
            _viewModel.SelectedLogItem = null;
        }
    }
}
