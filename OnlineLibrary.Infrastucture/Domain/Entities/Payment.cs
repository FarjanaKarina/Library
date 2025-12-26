using System.ComponentModel.DataAnnotations;

namespace OnlineLibrary.Infrastructure.Domain.Entities
{
    public class Payment
    {
        [Key]
        public Guid PaymentId { get; set; }

        public Guid UserId { get; set; }

        public decimal Amount { get; set; }

        public string Purpose { get; set; } // Membership / Fine

        public bool IsPaid { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
