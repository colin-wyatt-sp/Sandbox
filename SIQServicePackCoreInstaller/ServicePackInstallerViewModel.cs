using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;

namespace SIQServicePackCoreInstaller
{
    public class ServicePackInstallerViewModel : INotifyPropertyChanged
    {
        //private string logText;
        private string _servicePackLocation;
        private ObservableCollection<LogItem> _logItems = new ObservableCollection<LogItem>();
        private readonly object _lockObject = new object();

        public ServicePackInstallerViewModel() {

            BindingOperations.EnableCollectionSynchronization(_logItems, _lockObject);
        }

        public ObservableCollection<LogItem> LogItems {
            get { return _logItems; }
            set {
                _logItems = value;
                onPropertyChanged();
            }
        }

        public string ServicePackLocation {
            get { return _servicePackLocation; }
            set {
                _servicePackLocation = value;
                onPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void onPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
