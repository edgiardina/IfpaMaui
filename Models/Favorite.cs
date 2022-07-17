using SQLite;
using System;

namespace Ifpa.Models
{
    public class Favorite
    {
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }
        [Unique]
        public int PlayerID { get; set; }
        public DateTime CreatedDateTime { get; set; }
    }
}
