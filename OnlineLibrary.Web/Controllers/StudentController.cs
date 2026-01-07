using Microsoft.AspNetCore.Mvc;
using OnlineLibrary.Infrastructure.Data;
using OnlineLibrary.Web.Models;

namespace OnlineLibrary.Web.Controllers
{
    public class StudentController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        // =========================
        // STUDENT DASHBOARD
        // =========================
        public IActionResult Dashboard()
        {
            if (!IsStudent())
                return RedirectToAction("Login", "Account");

            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
                return RedirectToAction("Login", "Account");

            var userId = Guid.Parse(userIdString);
            var user = _context.Users.Find(userId);
            if (user == null)
                return RedirectToAction("Login", "Account");

            // =========================
            // ORDER STATISTICS
            // =========================
            var totalOrders = _context.Orders
                .Count(o => o.UserId == userId && o.PaymentStatus == "Success");

            var activeOrders = _context.Orders
                .Count(o => o.UserId == userId
                    && o.PaymentStatus == "Success"
                    && o.OrderStatus != "Delivered");

            var pendingReturns = (from o in _context.Orders
                                  join oi in _context.OrderItems on o.OrderId equals oi.OrderId
                                  where o.UserId == userId
                                      && (oi.Status == "ReturnRequested" || oi.Status == "ReturnApproved")
                                  select oi).Count();

            // =========================
            // RECENT ORDERS
            // =========================
            var recentOrders =
                (from o in _context.Orders
                 where o.UserId == userId
                 orderby o.OrderDate descending
                 select new OrderHistoryItem
                 {
                     OrderId = o.OrderId,
                     TransactionId = o.TransactionId ?? "",
                     OrderDate = o.OrderDate,
                     TotalAmount = o.TotalAmount,
                     OrderStatus = o.OrderStatus,
                     PaymentStatus = o.PaymentStatus,
                     ItemCount = _context.OrderItems.Count(oi => oi.OrderId == o.OrderId)
                 })
                .Take(5)
                .ToList();

            // =========================
            // CONTINUE READING
            // =========================
            var continueReading =
                (from w in _context.Wishlists
                 join b in _context.Books on w.BookId equals b.BookId
                 where w.UserId == userId && w.LastReadAt != null
                 orderby w.LastReadAt descending
                 select new ContinueReadingItem
                 {
                     BookId = b.BookId,
                     Title = b.Title,
                     ImageUrl = b.ImageUrl
                 })
                .Take(4)
                .ToList();

            // =========================
            // VIEW MODEL
            // =========================
            var model = new StudentDashboardViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                TotalOrders = totalOrders,
                ActiveOrders = activeOrders,
                PendingReturns = pendingReturns,
                RecentOrders = recentOrders,
                ContinueReading = continueReading
            };

            return View(model);
        }

        // =========================
        // BROWSE BOOKS
        // =========================
        public IActionResult BrowseBooks(string? search)
        {
            if (!IsStudent())
                return RedirectToAction("Login", "Account");

            var booksQuery =
                from b in _context.Books
                select new StudentBookViewModel
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    Author = b.Author,
                    ImageUrl = b.ImageUrl,
                    AvailableCopies = b.TotalCopies,
                    Price = b.Price
                };

            if (!string.IsNullOrWhiteSpace(search))
            {
                booksQuery = booksQuery.Where(b =>
                    (b.Title != null && b.Title.Contains(search)) ||
                    (b.Author != null && b.Author.Contains(search)));
            }

            return View(booksQuery.ToList());
        }

        // =========================
        // STUDENT NOTIFICATIONS
        // =========================
        public IActionResult Notifications()
        {
            if (!IsStudent())
                return RedirectToAction("Login", "Account");

            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Login", "Account");

            var userId = Guid.Parse(userIdStr);

            var notifications = _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();

            return View(notifications);
        }

        // =========================
        // ROLE CHECK: STUDENT
        // =========================
        private bool IsStudent()
        {
            var roleId = HttpContext.Session.GetString("RoleId");
            if (string.IsNullOrEmpty(roleId))
                return false;

            var roleName = _context.Roles
                .Where(r => r.RoleId == Guid.Parse(roleId))
                .Select(r => r.RoleName)
                .FirstOrDefault();

            return roleName == "Student";
        }
    }
}
