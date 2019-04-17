using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using ADTester.Annotations;
using ADTester.Interfaces;
using ADTester.Model.Data;

namespace ADTester {
    public class ActiveDirectoryTesterViewModel : INotifyPropertyChanged {

        private ObservableCollection<LogItem> _logItems = new ObservableCollection<LogItem>();
        private readonly object _lockObject = new object();
        private string _code;
        private string _outputText;
        private IActionResult _actionResult;
        private string _domain;
        private string _specificServer;
        private string _username;
        private string _domainNetbios;
        private IArbitraryAction _selectedAction;
        private LogItem _selectedLogItem;
        private bool _isCodeEditEnabled;

        public ActiveDirectoryTesterViewModel() {
            BindingOperations.EnableCollectionSynchronization(_logItems, _lockObject);

            ActionList = new ObservableCollection<IArbitraryAction>();
            string actionFilesDir =
                Path.Combine(new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName, "cfg");
            foreach (string file in Directory.GetFiles(actionFilesDir))
            {
                //OutputText += file + Environment.NewLine;
                ActionList.Add(new ArbitraryActiveDirectoryAction(new FileInfo(file).Name, File.ReadAllText(file)));
            }
        }

        public bool IsCodeEditEnabled
        {
            get => _isCodeEditEnabled;
            set
            {
                _isCodeEditEnabled = value;
                onPropertyChanged();
            }
        }

        public string Username
        {
            get => _username;
            set { _username = value;
                onPropertyChanged(); }
        }

        public string SpecificServer
        {
            get => _specificServer;
            set
            {
                _specificServer = value;
                onPropertyChanged();
            }
        }

        public string Domain
        {
            get => _domain;
            set
            {
                _domain = value;
                onPropertyChanged();
            }
        }

        public ObservableCollection<LogItem> LogItems {
            get { return _logItems; }
            set {
                _logItems = value;
                onPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void onPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableCollection<IArbitraryAction> ActionList { get; }

        public IArbitraryAction SelectedAction
        {
            get => _selectedAction;
            set
            {
                _selectedAction = value; 
                onPropertyChanged();
            }
        }

        public IActionResult ActionResult
        {
            get => _actionResult;
            set
            {
                _actionResult = value;
                onPropertyChanged();
            }
        }

        public string OutputText
        {
            get => _outputText;
            set
            {
                _outputText = value;
                onPropertyChanged();
            }
        }

        public string Code
        {
            get => _code;
            set
            {
                _code = value;
                onPropertyChanged();
            }
        }

        public string DomainNetbios
        {
            get => _domainNetbios;
            set
            {
                _domainNetbios = value; 
                onPropertyChanged();
            }
        }

        public LogItem SelectedLogItem
        {
            get => _selectedLogItem;
            set
            {
                _selectedLogItem = value; 
                onPropertyChanged();
            }
        }
    }
}