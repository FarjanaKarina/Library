using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineLibrary.Infrastructure.Domain.Entities
{
    public class Notification
    {
        [Key]
        public Guid NotificationId { get; set; }
        public Guid? UserId { get; set; }
        public string? Role { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; } = false;

        public string Type { get; set; } // info, warning, overdue, reminder

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
