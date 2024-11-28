namespace Ifpa.Models
{
    public class AppSettings
    {
        public string IfpaApiKey { get; set; }
        public string SyncFusionLicenseKey { get; set; }
        public string AppStoreAppId { get; set; }
        public string PlayStoreAppId { get; set; }
        public string IfpaRssFeedUrl { get; set; }
        public string IfpaPlayerNoProfilePicUrl { get; set; }

        public List<int> Sponsors { get; set; }
    }
}
