using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineLibrary.Infrastructure.Domain.Entities
{
    public class Book
    {
        [Key]
        public Guid BookId { get; set; }

        public Guid CategoryId { get; set; }

        public string Title { get; set; }
        public string Author { get; set; }
        public string Publisher { get; set; }
        public string Description { get; set; }
        public string ISBN { get; set; }

        public DateTime PurchaseDate { get; set; }
        public DateTime PublishDate { get; set; }

        public decimal Price { get; set; }

        public int TotalCopies { get; set; } = 1;

        public string? ImageUrl { get; set; }
        public string? PdfUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public double Rating { get; set; }
    }
}
