using System.ComponentModel.DataAnnotations;

namespace OnlineLibrary.Infrastructure.Domain.Entities
{
    public class AuditLog
    {
        [Key]
        public Guid AuditLogId { get; set; }

        public Guid ActorUserId { get; set; }   // who did it
        public string ActorRole { get; set; }   // Admin / Librarian

        public string Action { get; set; }      // e.g. "Book Added"
        public string EntityName { get; set; }  // Book / Membership
        public Guid EntityId { get; set; }

        public string Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
