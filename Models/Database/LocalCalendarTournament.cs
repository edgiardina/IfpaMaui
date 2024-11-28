using SQLite;

namespace Ifpa.Models.Database
{
    public class LocalCalendarTournament
    {
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }

        [Unique]
        public long TournamentID { get; set; }

        [Unique]
        public string LocalCalendarEventID { get; set; }
    }
}
