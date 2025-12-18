namespace OnlineLibrary.Web.Models
{
    public class BookDetailsViewModel
    {
        public Guid BookId { get; set; }

        public string Title { get; set; }
        public string Author { get; set; }
        public string Publisher { get; set; }
        public string ISBN { get; set; }

        public string Description { get; set; }

        public string CategoryName { get; set; }

        public DateTime PublishDate { get; set; }
        public decimal Price { get; set; }
        public int TotalCopies { get; set; }

        public string ImageUrl { get; set; }
    }
}
