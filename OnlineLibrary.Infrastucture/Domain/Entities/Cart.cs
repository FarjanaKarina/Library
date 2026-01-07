using System.ComponentModel.DataAnnotations;

namespace OnlineLibrary.Infrastructure.Domain.Entities
{
    public class Cart
    {
        [Key]
        public Guid CartId { get; set; }

        public Guid UserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}