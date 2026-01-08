namespace OnlineLibrary.Web.Models
{
    public class RefundViewModel
    {
        public Guid OrderItemId { get; set; }
        public Guid OrderId { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public decimal OriginalPrice { get; set; }
        public int Quantity { get; set; }
        public decimal RefundAmount { get; set; }
        public DateTime? RefundedAt { get; set; }
        public string? BookImageUrl { get; set; }
    }

    public class RefundSummaryViewModel
    {
        public int TotalRefunds { get; set; }
        public decimal TotalRefundAmount { get; set; }
        public int RefundsThisMonth { get; set; }
        public decimal RefundAmountThisMonth { get; set; }
        public List<RefundViewModel> Refunds { get; set; } = [];
    }
}