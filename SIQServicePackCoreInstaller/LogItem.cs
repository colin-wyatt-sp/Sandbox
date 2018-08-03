using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SIQServicePackCoreInstaller.VMs
{
    public abstract class LogItem : INotifyPropertyChanged {

        private string message;

        public string Message {
            get => message;
            set {
                message = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class LogInfoItem : LogItem {

    }

    public class LogWarnItem : LogItem {

    }

    public class LogErrorItem : LogItem {

    }
}
