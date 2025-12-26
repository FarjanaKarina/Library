namespace OnlineLibrary.Web.Models
{
    public class BookListViewModel
    {
        public Guid BookId { get; set; }

        public string? Title { get; set; }
        public string? Author { get; set; }

        public decimal Price { get; set; }
        public int TotalCopies { get; set; }

        public DateTime PurchaseDate { get; set; }

        public string? ImageUrl { get; set; }
        public List<string> Categories { get; set; } = new();

    }
}
