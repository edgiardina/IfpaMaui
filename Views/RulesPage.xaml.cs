using Ifpa.ViewModels;
using System;
using System.Collections;
using Microsoft.Maui;


namespace Ifpa.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RulesPage : ContentPage
    {
        private const string rulesPdfPath = "https://www.ifpapinball.com/wp/wp-content/uploads/2023/09/PAPA_IFPA-Complete-Competition-Rules-2023.09.pdf";

        public RulesPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            RulesWebView.Source = $"https://docs.google.com/viewer?embedded=true&url={rulesPdfPath}";
        }
    }
}