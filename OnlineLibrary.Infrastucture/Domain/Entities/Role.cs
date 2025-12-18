using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineLibrary.Infrastructure.Domain.Entities
{
    public class Role
    {
        [Key]
        public Guid RoleId { get; set; }
        public string? RoleName { get; set; }
    }
}
