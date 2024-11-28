using SQLite;

namespace Ifpa.Models.Database
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
