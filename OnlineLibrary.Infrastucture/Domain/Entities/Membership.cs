using System.ComponentModel.DataAnnotations;

namespace OnlineLibrary.Infrastructure.Domain.Entities
{
    public class Membership
    {
        [Key]
        public Guid MembershipId { get; set; }

        public Guid UserId { get; set; }

        // Pending | Approved | Rejected
        public string Status { get; set; } = "Pending";

        public DateTime AppliedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? ExpiryDate { get; set; }

        public bool IsActive { get; set; }
       
    }
}
