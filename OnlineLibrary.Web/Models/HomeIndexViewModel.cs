namespace OnlineLibrary.Web.Models
{
    public class HomeIndexViewModel
    {
        public List<FeaturedBookViewModel> FeaturedBooks { get; set; }
        public List<BestSellingBookViewModel> TopRatedBooks { get; set; }
        public List<PublicBookViewModel> ExploreBooks { get; set; }
        public List<PublicBookViewModel> AllBooks { get; set; }
        public List<CategoryViewModel> Categories { get; set; }
        public List<LibrarianViewModel> Librarians { get; set; }
    }

    public class CategoryViewModel
    {
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string Icon { get; set; } // Bootstrap icon class
    }

    public class LibrarianViewModel
    {
        public string FullName { get; set; }
        public string Role { get; set; }
        public string ImageUrl { get; set; }
    }
}
