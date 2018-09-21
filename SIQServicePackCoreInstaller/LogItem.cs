using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SIQServicePackCoreInstaller
{
    public abstract class LogItem : INotifyPropertyChanged {

        private string _message;

        public string Message {
            get => _message;
            set {
                _message = value;
                onPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void onPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class LogInfoItem : LogItem {

    }

    public class LogWarnItem : LogItem {

    }

    public class LogErrorItem : LogItem {

    }

    public class LogDebugItem : LogItem {

    }

}
