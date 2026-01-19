using System.ComponentModel.DataAnnotations;

namespace OnlineLibrary.Infrastructure.Domain.Entities
{
    public class Payment
    {
        [Key]
        public Guid PaymentId { get; set; }

        public Guid UserId { get; set; }
        public string Purpose { get; set; } = ""; 
        public string? TransactionId { get; set; }
        public string? SessionKey { get; set; }
        public string? ValidatorId { get; set; }
        public string? BankTransactionId { get; set; }
        public string PaymentMethod { get; set; } = "SSLCommerz";
        public decimal Amount { get; set; }
        public bool IsPaid { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
