namespace OnlineLibrary.Web.Models
{
    public class MyOrdersViewModel
    {
        public List<OrderSummaryViewModel> Orders { get; set; } = [];
    }

    public class OrderSummaryViewModel
    {
        public Guid OrderId { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public int TotalItems { get; set; }
        public bool CanRequestReturn { get; set; } // Only if delivered
    }

    public class OrderDetailsViewModel
    {
        // Order Info
        public Guid OrderId { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;

        // Shipping Info
        public string ShippingName { get; set; } = string.Empty;
        public string ShippingPhone { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;

        // Payment Info
        public string? BankTransactionId { get; set; }
        public string? CardType { get; set; }
        public DateTime? PaymentDate { get; set; }

        // Order Items
        public List<OrderItemDetailViewModel> Items { get; set; } = [];
    }

    public class OrderItemDetailViewModel
    {
        public Guid OrderItemId { get; set; }
        public Guid BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal Subtotal => Price * Quantity;

        // Return Info
        public DateTime? ReturnRequestedAt { get; set; }
        public DateTime? ReturnApprovedAt { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public DateTime? RefundedAt { get; set; }
        public decimal? RefundAmount { get; set; }

        // Actions
        public bool CanRequestReturn => Status == "Active";
        public bool CanCancelReturn => Status == "ReturnRequested";
    }
}