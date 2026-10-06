using Ifpa.ViewModels;
using PinballApi.Models.WPPR.Universal;
using PinballApi.Models.WPPR.Universal.Rankings;
using Plugin.Maui.NativeContextMenus;

namespace Ifpa.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RankingsPage : ContentPage
    {
        static readonly int[] PlayerCounts = [50, 100, 250, 500];

        RankingsViewModel ViewModel;

        readonly Dictionary<RankingType, MenuNode> rankingTypeNodes = new();
        readonly Dictionary<RankingSystem, MenuNode> rankingSystemNodes = new();
        readonly Dictionary<TournamentType, MenuNode> proRankingTypeNodes = new();
        readonly Dictionary<int, MenuNode> playerCountNodes = new();
        readonly MenuNode rankingSystemSection = new();
        readonly MenuNode proRankingTypeSection = new();
        readonly MenuNode countryMenu = new();
        readonly MenuNode pagingSection = new();
        readonly MenuNode playerCountMenu = new();
        readonly MenuNode startingRankNode = new();

        bool isLoading;
        bool isReloadPending;

        public RankingsPage(RankingsViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = ViewModel = viewModel;

            BuildFilterMenu();
        }

        void BuildFilterMenu()
        {
            var rankingTypeSection = new MenuNode();
            foreach (var type in ViewModel.RankingTypes)
            {
                rankingTypeNodes[type] = CreateRadioNode(type.ToString(), nameof(RankingType), () =>
                {
                    ViewModel.CurrentRankingType = type;
                    Preferences.Set("RankingType", type.ToString());
                });
                rankingTypeSection.Children.Add(rankingTypeNodes[type]);
            }

            foreach (var system in ViewModel.RankingSystems)
            {
                rankingSystemNodes[system] = CreateRadioNode(system.ToString(), nameof(RankingSystem), () =>
                {
                    ViewModel.CurrentRankingSystem = system;
                    Preferences.Set("RankingSystem", system.ToString());
                });
                rankingSystemSection.Children.Add(rankingSystemNodes[system]);
            }

            foreach (var type in ViewModel.ProRankingTypes)
            {
                proRankingTypeNodes[type] = CreateRadioNode(type.ToString(), nameof(TournamentType), () =>
                {
                    ViewModel.CurrentProRankingType = type;
                    Preferences.Set("ProRankingType", type.ToString());
                });
                proRankingTypeSection.Children.Add(proRankingTypeNodes[type]);
            }

            foreach (var count in PlayerCounts)
            {
                playerCountNodes[count] = CreateRadioNode(count.ToString(), "PlayerCount", () =>
                {
                    ViewModel.CountOfItemsToFetch = count;
                    Preferences.Set("PlayerCount", count);
                });
                playerCountMenu.Children.Add(playerCountNodes[count]);
            }

            startingRankNode.Tapped += async (_, _) => await PromptForStartingRank();

            pagingSection.Children.Add(playerCountMenu);
            pagingSection.Children.Add(startingRankNode);

            FilterMenu.Items.Add(rankingTypeSection);
            FilterMenu.Items.Add(rankingSystemSection);
            FilterMenu.Items.Add(proRankingTypeSection);
            FilterMenu.Items.Add(countryMenu);
            FilterMenu.Items.Add(pagingSection);

            UpdateFilterMenu();
        }

        MenuNode CreateRadioNode(string title, string groupKey, Action select)
        {
            var node = new MenuNode { Title = title, GroupKey = groupKey, IsCheckable = true };
            node.Tapped += async (_, _) =>
            {
                select();
                await ReloadRankings();
            };
            return node;
        }

        // The country list comes from the API, so the country nodes are built after the first load
        void BuildCountryNodes()
        {
            if (countryMenu.Children.Count == ViewModel.Countries.Count)
                return;

            countryMenu.Children.Clear();
            foreach (var country in ViewModel.Countries)
            {
                var node = CreateRadioNode(country.CountryName, nameof(Country), () =>
                {
                    ViewModel.CountryToShow = country;
                    Preferences.Set("CountryName", country.CountryName);
                });
                countryMenu.Children.Add(node);
            }
        }

        // Sets the menu to the current filter. The plugin reads the nodes each time the menu opens.
        void UpdateFilterMenu()
        {
            var type = ViewModel.CurrentRankingType;

            foreach (var (key, node) in rankingTypeNodes)
                node.IsChecked = key == type;
            foreach (var (key, node) in rankingSystemNodes)
                node.IsChecked = key == ViewModel.CurrentRankingSystem;
            // The default pro ranking type is Main, which is not a choice. It gives the open list.
            var proType = proRankingTypeNodes.ContainsKey(ViewModel.CurrentProRankingType)
                ? ViewModel.CurrentProRankingType
                : TournamentType.Open;
            foreach (var (key, node) in proRankingTypeNodes)
                node.IsChecked = key == proType;
            foreach (var (key, node) in playerCountNodes)
                node.IsChecked = key == ViewModel.CountOfItemsToFetch;
            foreach (var node in countryMenu.Children)
                node.IsChecked = node.Title == ViewModel.CountryToShow?.CountryName;

            rankingSystemSection.IsVisible = type == RankingType.Women;
            proRankingTypeSection.IsVisible = type == RankingType.Pro;

            countryMenu.Title = ViewModel.CountryToShow?.CountryName ?? Strings.RankingsFilterModalPage_Country;
            countryMenu.IsVisible = type == RankingType.Country && countryMenu.Children.Count > 0;

            // The pro rankings are one fixed list
            pagingSection.IsVisible = type != RankingType.Pro;
            playerCountMenu.Title = $"{Strings.RankingsFilterModalPage_Players}: {ViewModel.CountOfItemsToFetch}";
            startingRankNode.Title = $"{Strings.RankingsFilterModalPage_StartingRank}: {ViewModel.StartingPosition}";
        }

        async Task PromptForStartingRank()
        {
            var answer = await DisplayPromptAsync(
                Strings.RankingsFilterModalPage_StartingRank,
                null,
                Strings.OK,
                Strings.Cancel,
                keyboard: Keyboard.Numeric,
                initialValue: ViewModel.StartingPosition.ToString());

            if (!int.TryParse(answer, out var startingRank) || startingRank < 1)
                return;

            ViewModel.StartingPosition = startingRank;
            Preferences.Set("StartingRank", startingRank);
            await ReloadRankings();
        }

        // A filter change during a load starts one more load, so the list always shows the last filter
        async Task ReloadRankings()
        {
            UpdateFilterMenu();

            isReloadPending = true;
            if (isLoading)
                return;

            isLoading = true;
            try
            {
                while (isReloadPending)
                {
                    isReloadPending = false;
                    await ViewModel.LoadItems();
                }
            }
            finally
            {
                isLoading = false;
            }

            BuildCountryNodes();
            UpdateFilterMenu();
        }

        protected override void OnAppearing()
        {
            if (ViewModel.Players.Count == 0)
            {
                ViewModel.CountOfItemsToFetch = Preferences.Get("PlayerCount", ViewModel.CountOfItemsToFetch);
                ViewModel.StartingPosition = Preferences.Get("StartingRank", ViewModel.StartingPosition);

                // if ranking type preference is 'Main', reset to 'Wppr' type
                if (Preferences.Get("RankingType", ViewModel.CurrentRankingType.ToString()) == "Main")
                {
                    Preferences.Set("RankingType", "Wppr");
                }

                ViewModel.CurrentRankingType = (RankingType)Enum.Parse(typeof(RankingType), Preferences.Get("RankingType", ViewModel.CurrentRankingType.ToString()));
                ViewModel.CurrentRankingSystem = (RankingSystem)Enum.Parse(typeof(RankingSystem), Preferences.Get("RankingSystem", ViewModel.CurrentRankingSystem.ToString()));
                ViewModel.CurrentProRankingType = (TournamentType)Enum.Parse(typeof(TournamentType), Preferences.Get("ProRankingType", ViewModel.CurrentProRankingType.ToString()));

                ViewModel.CountryToShow = new Country { CountryName = Preferences.Get("CountryName", ViewModel.DefaultCountry.CountryName) };

                _ = ReloadRankings();
            }
            base.OnAppearing();
        }
    }
}