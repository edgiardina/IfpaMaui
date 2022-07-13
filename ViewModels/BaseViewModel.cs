using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Ifpa.Models;
using IfpaMaui.Models;
using Microsoft.Extensions.Configuration;
using PinballApi;

namespace Ifpa.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        IConfiguration configuration;

        public PinballRankingApiV1 PinballRankingApi { get; private set; } 
        public PinballRankingApiV2 PinballRankingApiV2 { get; private set; } 

        bool isBusy = false;
        public bool IsBusy
        {
            get { return isBusy; }
            set { SetProperty(ref isBusy, value); }
        }

        string title = string.Empty;
        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }

        public BaseViewModel(IConfiguration config)
        {
            configuration = config;

            var settings = configuration.GetRequiredSection("AppSettings").Get<AppSettings>();

            PinballRankingApi = new PinballRankingApiV1(settings.IfpaApiKey);
            PinballRankingApiV2 = new PinballRankingApiV2(settings.IfpaApiKey);
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
