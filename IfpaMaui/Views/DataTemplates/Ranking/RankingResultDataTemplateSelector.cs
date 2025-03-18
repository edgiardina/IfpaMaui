using PinballApiRankings = PinballApi.Models.WPPR.Universal.Rankings;

namespace Ifpa.Views.DataTemplates.Ranking
{
    internal class RankingResultDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate RankingResultTemplate { get; set; }      
        public DataTemplate ProRankingTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            if(item != null)
            {
                if (item is PinballApiRankings.ProRanking)
                    return ProRankingTemplate;               
                else if (item is PinballApiRankings.Ranking)
                    return RankingResultTemplate;
            }

            return null;
        }
    }
}
