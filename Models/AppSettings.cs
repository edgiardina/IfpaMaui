using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IfpaMaui.Models
{
    public class AppSettings
    {
        public string IfpaApiKey { get; set; }
        public string SyncFusionLicenseKey { get; set; }
        public string AppStoreAppId { get; set; }
        public string PlayStoreAppId { get; set; }
        public string IfpaRssFeedUrl { get; set; }
        public string PlayerProfileNoPicUrl { get; set; }
    }
}
