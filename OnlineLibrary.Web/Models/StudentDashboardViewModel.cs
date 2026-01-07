namespace OnlineLibrary.Web.Models
{
    public class StudentDashboardViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Order Statistics
        public int TotalOrders { get; set; }
        public int ActiveOrders { get; set; }
        public int PendingReturns { get; set; }

        // Recent Orders
        public List<OrderHistoryItem> RecentOrders { get; set; } = [];

        // Continue Reading
        public List<ContinueReadingItem> ContinueReading { get; set; } = [];
    }

    public class OrderHistoryItem
    {
        public Guid OrderId { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public int ItemCount { get; set; }
    }

    public class ContinueReadingItem
    {
        public Guid BookId { get; set; } 
        public string Title { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }
}
