namespace OnlineLibrary.Web.Models
{
    public class LibrarianDashboardViewModel
    {
        public int TotalBooks { get; set; }
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int PendingReturns { get; set; }
        public List<RecentOrderViewModel> RecentOrders { get; set; } = [];
        public List<ReturnRequestViewModel> PendingReturnRequests { get; set; } = [];
    }

    public class RecentOrderViewModel
    {
        public Guid OrderId { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public int ItemCount { get; set; }
    }

    public class ReturnRequestViewModel
    {
        public Guid OrderItemId { get; set; }
        public Guid OrderId { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty; // Added
        public string BookTitle { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; } // Added
        public string Status { get; set; } = string.Empty; // Added
        public DateTime? ReturnRequestedAt { get; set; }
        public DateTime? ReturnApprovedAt { get; set; } // Added

        public decimal RefundAmount => Price * Quantity * 0.5m; // Helper logic
    }
}