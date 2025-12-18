namespace OnlineLibrary.Web.Models
{
    public class PublicBookViewModel
    {
        public Guid BookId { get; set; }

        public string Title { get; set; }
        public string Author { get; set; }

        public string CategoryName { get; set; }

        public string ImageUrl { get; set; }
    }
}
