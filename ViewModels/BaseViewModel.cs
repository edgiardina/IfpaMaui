using System.ComponentModel;
using System.Runtime.CompilerServices;
using PinballApi;

namespace Ifpa.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        public PinballRankingApiV1 PinballRankingApi { get; private set; } 
        public PinballRankingApiV2 PinballRankingApiV2 { get; private set; } 

        bool isBusy = false;
        public bool IsBusy
        {
            get { return isBusy; }
            set { SetProperty(ref isBusy, value); }
        }

        bool isLoaded = false;
        public bool IsLoaded
        {
            get { return isLoaded; }
            set { SetProperty(ref isLoaded, value); }
        }

        string title = string.Empty;
        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }

        public BaseViewModel(PinballRankingApiV1 pinballRankingApiV1, PinballRankingApiV2 pinballRankingApiV2)
        {         

            PinballRankingApi = pinballRankingApiV1;
            PinballRankingApiV2 = pinballRankingApiV2;
        }

        protected bool SetProperty<T>(ref T backingStore, T value,
            [CallerMemberName]string propertyName = "",
            Action onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            var changed = PropertyChanged;
            if (changed == null)
                return;

            changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
