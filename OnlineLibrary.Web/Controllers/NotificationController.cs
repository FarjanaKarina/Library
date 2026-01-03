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
           // Parse uidStr to Guid
            var uid = Guid.Parse(uidStr);
            var notifications = _context.Notifications
     .Where(n => n.UserId == uid)
     .OrderByDescending(n => n.CreatedAt)
     .ToList();

            var unread = notifications.Where(n => !n.IsRead).ToList();

            foreach (var n in unread)
            {
                n.IsRead = true;
            }

            _context.SaveChanges();

            return View(notifications);
        }

    }
}