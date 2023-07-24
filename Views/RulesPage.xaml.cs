using Ifpa.ViewModels;
using System;
using System.Collections;
using Microsoft.Maui;


namespace Ifpa.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RulesPage : ContentPage
    { 

        public RulesPage()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);

            RulesWebView.Source = "https://docs.google.com/viewer?embedded=true&url=https://www.ifpapinball.com/wp/wp-content/uploads/2021/04/PAPA_IFPA-Complete-Competition-Rules-2021.04.06.pdf";
        }
    }
}