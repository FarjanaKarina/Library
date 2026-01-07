namespace OnlineLibrary.Web.Models
{
    /// <summary>
    /// Main view model for the reports page.
    /// </summary>
    public class ReportViewModel
    {
        public ChartData? MonthlySales { get; set; }
        public ChartData? CategoryDistribution { get; set; }
        public List<BookPerformanceViewModel>? TopSellingBooks { get; set; }
        public ReportMetricsViewModel? Metrics { get; set; }
    }

    /// <summary>
    /// Represents data structured for Chart.js.
    /// </summary>
    public class ChartData
    {
        public List<string> Labels { get; set; } = [];
        public List<decimal> Data { get; set; } = [];
    }

    /// <summary>
    /// Represents the performance of a single book.
    /// </summary>
    public class BookPerformanceViewModel
    {
        public string Title { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public string? ImageUrl { get; set; }
    }

    /// <summary>
    /// Holds key metrics for the report dashboard.
    /// </summary>
    public class ReportMetricsViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRefunds { get; set; }
        public int ReturnedItems { get; set; }
    }
}