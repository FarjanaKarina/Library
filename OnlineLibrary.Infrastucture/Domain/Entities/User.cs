using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineLibrary.Infrastructure.Domain.Entities
{
    public class User
    {
        [Key]
        public Guid UserId { get; set; }

        public string FullName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }

        public string Phone { get; set; }
        public string Address { get; set; }

        public Guid RoleId { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
