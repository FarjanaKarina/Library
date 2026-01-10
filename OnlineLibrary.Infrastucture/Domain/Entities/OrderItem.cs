using System.ComponentModel.DataAnnotations;

namespace OnlineLibrary.Infrastructure.Domain.Entities
{
    public class OrderItem
    {
        [Key]
        public Guid OrderItemId { get; set; }

        public Guid OrderId { get; set; }
        public Guid BookId { get; set; }

        public string BookTitle { get; set; }  // Snapshot at time of order
        public decimal Price { get; set; }     // Price at time of order
        public int Quantity { get; set; } = 1;

        // Active | ReturnRequested | ReturnApproved | Received | Refunded
        public string Status { get; set; } = "Active";

        public DateTime? ReturnRequestedAt { get; set; }
        public DateTime? ReturnApprovedAt { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public DateTime? RefundedAt { get; set; }

        public decimal? RefundAmount { get; set; }  // 50% of Price when refunded

        public string? RefundAccountNumber { get; set; }
        public string? RefundPaymentMethod { get; set; }
    }
}