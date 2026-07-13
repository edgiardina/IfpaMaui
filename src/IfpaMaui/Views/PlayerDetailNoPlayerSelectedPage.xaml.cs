using CommunityToolkit.Mvvm.Input;
using Ifpa.ViewModels;

namespace Ifpa.Views;

public partial class PlayerDetailNoPlayerSelectedPage : ContentPage
{
	PlayerDetailNoPlayerSelectedViewModel PlayerDetailNoPlayerSelectedViewModel;

    public PlayerDetailNoPlayerSelectedPage(PlayerDetailNoPlayerSelectedViewModel playerDetailNoPlayerSelectedViewModel)
	{
		InitializeComponent();

        BindingContext = PlayerDetailNoPlayerSelectedViewModel = playerDetailNoPlayerSelectedViewModel;
    }
}