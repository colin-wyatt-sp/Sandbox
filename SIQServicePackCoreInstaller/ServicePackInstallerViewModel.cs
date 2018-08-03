using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using SIQServicePackCoreInstaller.VMs;

namespace SIQServicePackCoreInstaller
{
    public class ServicePackInstallerViewModel : INotifyPropertyChanged
    {
        //private string logText;
        private string servicePackLocation;
        private ObservableCollection<LogItem> logItems = new ObservableCollection<LogItem>();
        private readonly object lockObject = new object();

        public ServicePackInstallerViewModel() {

            BindingOperations.EnableCollectionSynchronization(logItems, lockObject);
        }

        public ObservableCollection<LogItem> LogItems {
            get { return logItems; }
            set {
                logItems = value;
                OnPropertyChanged();
            }
        }

        public string ServicePackLocation {
            get { return servicePackLocation; }
            set {
                servicePackLocation = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
