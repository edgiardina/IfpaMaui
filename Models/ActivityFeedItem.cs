using SQLite;
using System;

namespace Ifpa.Models
{
    public class ActivityFeedItem
    {
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }
        public ActivityFeedType ActivityType { get; set; }
        public DateTime CreatedDateTime { get; set; }
        //Related ID of a tournament, or other ID needed depending on ActivityType
        public int? RecordID { get; set; }
        public string Description { get; set; }
        //General purpose integer values depending on ActivityType   
        public int? IntOne { get; set; }
        public int? IntTwo { get; set; }
        public bool HasBeenSeen { get; set; }
    }
}
