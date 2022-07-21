using Ifpa.ViewModels;
using System;
using System.Collections;
using Microsoft.Maui;


namespace Ifpa.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RulesPage : ContentPage
    {
        //RulesViewModel viewModel;        

        public RulesPage()
        {
            InitializeComponent();

            //BindingContext = this.viewModel = new RulesViewModel();
            //viewModel.LoadItemsCommand.Execute(null);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            RulesWebView.Source = "https://docs.google.com/viewer?embedded=true&url=https://www.ifpapinball.com/wp/wp-content/uploads/2021/04/PAPA_IFPA-Complete-Competition-Rules-2021.04.06.pdf";
        }
    }
}