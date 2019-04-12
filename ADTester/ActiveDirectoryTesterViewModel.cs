using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using ADTester.Annotations;

namespace ADTester {
    public class ActiveDirectoryTesterViewModel : INotifyPropertyChanged {

        private ObservableCollection<LogItem> _logItems = new ObservableCollection<LogItem>();
        private readonly object _lockObject = new object();

        public ActiveDirectoryTesterViewModel() {
            BindingOperations.EnableCollectionSynchronization(_logItems, _lockObject);
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
    }
}