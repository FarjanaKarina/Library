namespace OnlineLibrary.Web.Models
{
    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = [];
        public decimal TotalAmount => Items.Sum(i => i.Subtotal);
        public int TotalItems => Items.Sum(i => i.Quantity);
    }

    public class CartItemViewModel
    {
        public Guid CartItemId { get; set; }
        public Guid BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int AvailableCopies { get; set; }
        public decimal Subtotal => Price * Quantity;
    }
}