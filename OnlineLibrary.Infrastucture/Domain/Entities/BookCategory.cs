using System.ComponentModel.DataAnnotations;

namespace OnlineLibrary.Infrastructure.Domain.Entities
{
    public class BookCategory
    {
        [Key]
        public Guid BookCategoryId { get; set; }

        public Guid BookId { get; set; }
        public Guid CategoryId { get; set; }
    }
}
