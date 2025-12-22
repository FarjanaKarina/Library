namespace OnlineLibrary.Web.Models
{
    public class LibrarianMembershipRequestViewModel
    {
        public Guid MembershipId { get; set; }
        public string StudentName { get; set; }
        public string Email { get; set; }
        public DateTime AppliedAt { get; set; }
    }

}
