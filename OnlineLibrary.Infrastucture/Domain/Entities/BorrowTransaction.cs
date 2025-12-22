using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineLibrary.Infrastructure.Domain.Entities
{
    public class BorrowTransaction
    {
        [Key]
        public Guid BorrowId { get; set; }

        public Guid BookId { get; set; }
        public Guid UserId { get; set; }

        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }

        public bool IsReturned { get; set; }
        public decimal FineAmount { get; set; }
        public bool IsFinePaid { get; set; } = false;

    }
}
