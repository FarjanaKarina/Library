using System.ComponentModel.DataAnnotations;

namespace OnlineLibrary.Infrastructure.Domain.Entities
{
    public class Wishlist
    {
        [Key]
        public Guid WishlistId { get; set; }

        public Guid UserId { get; set; }
        public Guid BookId { get; set; }
        public DateTime? LastReadAt { get; set; }
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
