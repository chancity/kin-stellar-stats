namespace kin_stellar_stats.Database.Models
{
    public class Pagination
    {
        public string CursorType { get; set; }
        public long PagingToken { get; set; }
    }
}
