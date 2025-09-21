using PinballApiRankings = PinballApi.Models.WPPR.Universal.Rankings;
using PinballApiPlayers = PinballApi.Models.WPPR.Universal.Players;

namespace Ifpa.Views.DataTemplates.Ranking
{
    internal class RankingResultDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate RankingResultTemplate { get; set; }      
        public DataTemplate ProRankingTemplate { get; set; }
        public DataTemplate PlayerSearchTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            if(item != null)
            {
                System.Diagnostics.Debug.WriteLine($"Template selector: item type = {item.GetType().FullName}");
                
                if (item is PinballApiRankings.ProRanking)
                {
                    System.Diagnostics.Debug.WriteLine("Selected ProRankingTemplate");
                    return ProRankingTemplate;
                }               
                else if (item is PinballApiRankings.Ranking)
                {
                    System.Diagnostics.Debug.WriteLine("Selected RankingResultTemplate");
                    return RankingResultTemplate;
                }
                else if (item is PinballApiPlayers.Player)
                {
                    System.Diagnostics.Debug.WriteLine("Selected PlayerSearchTemplate for Player");
                    return PlayerSearchTemplate;
                }
                else if (item is PinballApiPlayers.Search.PlayerSearchResult)
                {
                    System.Diagnostics.Debug.WriteLine("Selected PlayerSearchTemplate for PlayerSearchResult");
                    return PlayerSearchTemplate;
                }
                
                System.Diagnostics.Debug.WriteLine($"No template found for type: {item.GetType().FullName}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Template selector: item is null");
            }

            return null;
        }
    }
}
