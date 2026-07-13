using Ifpa.ViewModels;

namespace Ifpa.Views;

public partial class TournamentSearchPage : ContentPage
{
	private readonly TournamentSearchViewModel tournamentSearchViewModel;

    public TournamentSearchPage(TournamentSearchViewModel tournamentSearchViewModel)
	{
		this.BindingContext = this.tournamentSearchViewModel = tournamentSearchViewModel;

        InitializeComponent();
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await tournamentSearchViewModel.TournamentSearch();
    }
}