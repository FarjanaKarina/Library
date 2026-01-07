namespace OnlineLibrary.Web.Models
{
    public class CheckoutViewModel
    {
        // Cart Summary
        public List<CartItemViewModel> CartItems { get; set; } = [];
        public decimal TotalAmount => CartItems.Sum(i => i.Subtotal);
        public int TotalItems => CartItems.Sum(i => i.Quantity);

        // Shipping Info (Pre-filled from User)
        public string ShippingName { get; set; } = string.Empty;
        public string ShippingPhone { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
    }

    public class PlaceOrderViewModel
    {
        public string ShippingName { get; set; } = string.Empty;
        public string ShippingPhone { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
    }
}