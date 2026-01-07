using System.ComponentModel.DataAnnotations;

namespace OnlineLibrary.Infrastructure.Domain.Entities
{
    public class CartItem
    {
        [Key]
        public Guid CartItemId { get; set; }

        public Guid CartId { get; set; }
        public Guid BookId { get; set; }

        public int Quantity { get; set; } = 1;

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}