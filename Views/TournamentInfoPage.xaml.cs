using Ifpa.ViewModels;

namespace Ifpa.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TournamentInfoPage : ContentPage
    {
        public TournamentInfoPage(TournamentInfoViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = viewModel;
        }
    }
}