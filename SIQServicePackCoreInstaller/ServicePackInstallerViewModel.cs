using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using SIQServicePackCoreInstaller.Annotations;

namespace SIQServicePackCoreInstaller
{
    public class ServicePackInstallerViewModel : INotifyPropertyChanged
    {
        private string logText;
        private string servicePackLocation;

        public string LogText {
            get {
                return logText;
            }
            set {
                logText = value;
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

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
