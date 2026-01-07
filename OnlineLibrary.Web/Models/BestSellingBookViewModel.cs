namespace OnlineLibrary.Web.Models
{
    public class BestSellingBookViewModel
    {
        public Guid BookId { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
    }

}
