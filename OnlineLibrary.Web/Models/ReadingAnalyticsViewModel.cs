namespace OnlineLibrary.Web.Models
{
    public class ReadingAnalyticsViewModel
    {
        public int TotalActiveReaders { get; set; }
        public int TotalBooksBeingRead { get; set; }
        public int ReadersToday { get; set; }
        public int ReadersThisWeek { get; set; }

        public List<BookReadingStats> BookStats { get; set; } = [];
    }

    public class BookReadingStats
    {
        public Guid BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int ReaderCount { get; set; }
        public DateTime? LastReadTime { get; set; }
        public List<ReaderInfo> Readers { get; set; } = [];
    }

    public class ReaderInfo
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime? LastReadAt { get; set; }
    }
}