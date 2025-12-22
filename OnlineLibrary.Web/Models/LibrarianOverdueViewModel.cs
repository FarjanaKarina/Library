namespace OnlineLibrary.Web.Models
{
    public class LibrarianOverdueViewModel
    {
        public Guid BorrowId { get; set; }
        public string StudentName { get; set; }
        public string BookTitle { get; set; }
        public DateTime DueDate { get; set; }
        public int OverdueDays { get; set; }
        public decimal FineAmount { get; set; }
    }

}
