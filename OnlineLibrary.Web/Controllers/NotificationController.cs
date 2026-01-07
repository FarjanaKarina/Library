using Microsoft.AspNetCore.Mvc;
using OnlineLibrary.Infrastructure.Data;

namespace OnlineLibrary.Web.Controllers
{
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // NOTIFICATION INDEX PAGE
        // =========================
        public IActionResult Index()
        {
            var uidStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(uidStr))
                return RedirectToAction("Login", "Account");

            var uid = Guid.Parse(uidStr);

            // Get all notifications for this user
            var notifications = _context.Notifications
                .Where(n => n.UserId == uid)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();

            // Store which notification IDs were unread BEFORE marking them as read
            // These will be highlighted as "NEW" on this page load only
            var unreadIds = notifications
                .Where(n => !n.IsRead)
                .Select(n => n.NotificationId)
                .ToHashSet();

            // Pass unread IDs to the view so it can highlight them
            ViewBag.UnreadIds = unreadIds;

            // Now mark all unread notifications as read
            // Next time the user visits, these will no longer be highlighted
            var unreadNotifications = notifications.Where(n => !n.IsRead).ToList();
            foreach (var n in unreadNotifications)
            {
                n.IsRead = true;
            }
            _context.SaveChanges();

            return View(notifications);
        }

        // =========================
        // MARK ALL AS READ (AJAX)
        // =========================
        [HttpPost]
        public IActionResult MarkAllAsRead()
        {
            var uidStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(uidStr))
                return Unauthorized();

            var uid = Guid.Parse(uidStr);

            var unreadNotifications = _context.Notifications
                .Where(n => n.UserId == uid && !n.IsRead)
                .ToList();

            foreach (var n in unreadNotifications)
            {
                n.IsRead = true;
            }

            _context.SaveChanges();

            return Ok(new { success = true, markedCount = unreadNotifications.Count });
        }
    }
}