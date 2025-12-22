namespace OnlineLibrary.Web.Models
{
    public class LibrarianDashboardViewModel
    {
        public List<LibrarianOverdueViewModel> OverdueBorrows { get; set; }
        public List<LibrarianMembershipRequestViewModel> PendingMemberships { get; set; }
    }

}
