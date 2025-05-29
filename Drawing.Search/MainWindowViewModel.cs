using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Drawing.Search
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private string _version;
        public string Version
        {
            get => _version;
            set
            {
                _version = value;
                OnPropertyChanged();
            }
        }

        public MainWindowViewModel()
        {
            Version = $"v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}";
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}