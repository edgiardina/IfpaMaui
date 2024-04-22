using PinballApi.Models.WPPR.v2.Rankings;

namespace Ifpa.Views.DataTemplates.Ranking
{
    internal class RankingResultDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate RankingResultTemplate { get; set; }
        public DataTemplate CountryRankingTemplate { get; set; }
        public DataTemplate WomensRankingTemplate { get; set; }
        public DataTemplate YouthRankingTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            if(item != null)
            {
                if (item is CountryRankingResult)
                    return CountryRankingTemplate;
                else if (item is WomensRankingResult)
                    return WomensRankingTemplate;
                else if (item is YouthRankingResult)
                    return YouthRankingTemplate;
                else if (item is RankingResult)
                    return RankingResultTemplate;
            }

            return null;
        }
    }
}
