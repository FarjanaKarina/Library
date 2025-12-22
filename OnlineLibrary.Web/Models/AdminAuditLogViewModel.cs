namespace OnlineLibrary.Web.Models
{
    public class AdminAuditLogViewModel
    {
        public string ActorName { get; set; }
        public string ActorRole { get; set; }
        public string Action { get; set; }
        public string EntityName { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
