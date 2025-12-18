using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineLibrary.Infrastructure.Domain.Entities
{
    public class Fine
    {
        [Key]
        public Guid FineId { get; set; }

        public Guid BorrowId { get; set; }

        public int LateDays { get; set; }
        public decimal FineAmount { get; set; }

        public bool IsPaid { get; set; }
    }
}
