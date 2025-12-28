namespace OnlineLibrary.Web.Models
{
    public class FeaturedBookViewModel
    {
        public Guid BookId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string PdfUrl { get; set; }
    }

}
