namespace OnlineLibrary.Web.Models
{
    public class StudentDashboardViewModel
    {
        public string FullName { get; set; }
        public string Email { get; set; }

        public string MembershipStatus { get; set; }

        public List<BorrowHistoryItem> BorrowHistory { get; set; }
            = new();
    }

    public class BorrowHistoryItem
    {
        public Guid BorrowId { get; set; }
        public Guid BookId { get; set; }
        public string BookTitle { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; }
        public decimal FineAmount { get; set; }

    }
}
