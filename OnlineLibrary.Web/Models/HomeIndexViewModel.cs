namespace OnlineLibrary.Web.Models
{
    public class HomeIndexViewModel
    {
        public List<FeaturedBookViewModel> FeaturedBooks { get; set; }
        public List<BestSellingBookViewModel> TopRatedBooks { get; set; }
        public List<PublicBookViewModel> ExploreBooks { get; set; }
        public List<PublicBookViewModel> AllBooks { get; set; }
    }
}
