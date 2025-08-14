using PinballApi.Models.WPPR.Universal.Players;
using System.Collections.ObjectModel;

namespace Ifpa.Models
{
    public class PlayerVersusRecordGroup : ObservableCollection<PlayerVersusRecord>
    {
        public string Key { get; }

        public PlayerVersusRecordGroup(string key, IEnumerable<PlayerVersusRecord> items)
            : base(items)
        {
            Key = key;
        }
    }
}
