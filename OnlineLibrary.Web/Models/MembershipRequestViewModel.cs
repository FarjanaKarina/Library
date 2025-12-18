namespace OnlineLibrary.Web.Models
{
    public class MembershipRequestViewModel
    {
        public Guid MembershipId { get; set; }

        public string FullName { get; set; }
        public string Email { get; set; }

        public string Status { get; set; }
        public DateTime AppliedAt { get; set; }
    }
}
