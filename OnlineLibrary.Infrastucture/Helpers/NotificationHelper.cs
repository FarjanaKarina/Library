using OnlineLibrary.Infrastructure.Data;
using OnlineLibrary.Infrastructure.Domain.Entities;

namespace OnlineLibrary.Infrastructure.Helpers
{
    public static class NotificationHelper
    {
        // Send to one user
        public static void Send(
            ApplicationDbContext context,
            Guid userId,
            string title,
            string message,
            string type = "info")
        {
            context.Notifications.Add(new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            context.SaveChanges();
        }

        // Broadcast to all users
        public static void Broadcast(
            ApplicationDbContext context,
            string title,
            string message,
            string type = "system")
        {
            var userIds = context.Users.Select(u => u.UserId).ToList();

            foreach (var uid in userIds)
            {
                context.Notifications.Add(new Notification
                {
                    NotificationId = Guid.NewGuid(),
                    UserId = uid,
                    Title = title,
                    Message = message,
                    Type = type,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            context.SaveChanges();
        }
    }
}
