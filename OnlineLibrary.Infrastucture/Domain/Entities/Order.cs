using System.ComponentModel.DataAnnotations;

namespace OnlineLibrary.Infrastructure.Domain.Entities
{
    public class Order
    {
        [Key]
        public Guid OrderId { get; set; }

        public Guid UserId { get; set; }

        public decimal TotalAmount { get; set; }

        // Pending | Confirmed | Packed | Shipped | Delivered
        public string OrderStatus { get; set; } = "Pending";

        // Shipping Details
        public string ShippingName { get; set; }
        public string ShippingPhone { get; set; }
        public string ShippingAddress { get; set; }

        // Payment (SSLCommerz)
        public string? TransactionId { get; set; }
        public string? SessionKey { get; set; }
        public string PaymentStatus { get; set; } = "Pending"; // Pending, Success, Failed, Cancelled
        public string? BankTransactionId { get; set; }
        public string? CardType { get; set; }
        public DateTime? PaymentDate { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
    }
}