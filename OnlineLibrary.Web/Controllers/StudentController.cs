using Microsoft.AspNetCore.Mvc;
using OnlineLibrary.Infrastructure.Data;
using OnlineLibrary.Infrastructure.Security;
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
        // PROFILE (GET)
        // =========================
        public IActionResult Profile()
        {
            if (!IsStudent())
                return RedirectToAction("Login", "Account");

            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Login", "Account");

            var userId = Guid.Parse(userIdStr);
            var user = _context.Users.Find(userId);

            if (user == null)
                return RedirectToAction("Login", "Account");

            var model = new StudentProfileViewModel
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                CreatedAt = user.CreatedAt
            };

            return View(model);
        }

        // =========================
        // PROFILE (POST) - SAVE CHANGES
        // =========================
        [HttpPost]
        public IActionResult Profile(StudentProfileViewModel model)
        {
            if (!IsStudent())
                return RedirectToAction("Login", "Account");

            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Login", "Account");

            var userId = Guid.Parse(userIdStr);
            var user = _context.Users.Find(userId);

            if (user == null)
                return RedirectToAction("Login", "Account");

            // =========================
            // VALIDATION
            // =========================
            if (string.IsNullOrWhiteSpace(model.FullName))
            {
                ViewBag.Error = "Full name is required.";
                model.CreatedAt = user.CreatedAt;
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ViewBag.Error = "Email is required.";
                model.CreatedAt = user.CreatedAt;
                return View(model);
            }

            // Check if email is already taken by another user
            var emailExists = _context.Users
                .Any(u => u.Email == model.Email && u.UserId != userId);

            if (emailExists)
            {
                ViewBag.Error = "This email is already in use by another account.";
                model.CreatedAt = user.CreatedAt;
                return View(model);
            }

            // =========================
            // PASSWORD CHANGE (OPTIONAL)
            // =========================
            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                if (string.IsNullOrEmpty(model.CurrentPassword))
                {
                    ViewBag.Error = "Current password is required to change password.";
                    model.CreatedAt = user.CreatedAt;
                    return View(model);
                }

                var currentHash = PasswordHelper.HashPassword(model.CurrentPassword);
                if (user.PasswordHash != currentHash)
                {
                    ViewBag.Error = "Current password is incorrect.";
                    model.CreatedAt = user.CreatedAt;
                    return View(model);
                }

                if (model.NewPassword != model.ConfirmPassword)
                {
                    ViewBag.Error = "New password and confirm password do not match.";
                    model.CreatedAt = user.CreatedAt;
                    return View(model);
                }

                if (model.NewPassword.Length < 6)
                {
                    ViewBag.Error = "New password must be at least 6 characters.";
                    model.CreatedAt = user.CreatedAt;
                    return View(model);
                }

                user.PasswordHash = PasswordHelper.HashPassword(model.NewPassword);
            }

            // =========================
            // UPDATE USER
            // =========================
            user.FullName = model.FullName;
            user.Email = model.Email;
            user.Phone = model.Phone;
            user.Address = model.Address;

            _context.SaveChanges();

            ViewBag.Success = "Profile updated successfully!";
            model.CreatedAt = user.CreatedAt;

            // Clear password fields
            model.CurrentPassword = null;
            model.NewPassword = null;
            model.ConfirmPassword = null;

            return View(model);
        }

        // =========================
        // REFUND HISTORY (STUDENT)
        // =========================
        public IActionResult Refunds()
        {
            if (!IsStudent())
                return RedirectToAction("Login", "Account");

            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Login", "Account");

            var userId = Guid.Parse(userIdStr);

            var refunds = (from oi in _context.OrderItems
                           join o in _context.Orders on oi.OrderId equals o.OrderId
                           join b in _context.Books on oi.BookId equals b.BookId
                           where o.UserId == userId && oi.Status == "Refunded"
                           orderby oi.RefundedAt descending
                           select new RefundViewModel
                           {
                               OrderItemId = oi.OrderItemId,
                               OrderId = o.OrderId,
                               TransactionId = o.TransactionId ?? "",
                               BookTitle = oi.BookTitle,
                               OriginalPrice = oi.Price,
                               Quantity = oi.Quantity,
                               RefundAmount = oi.RefundAmount ?? 0,
                               RefundedAt = oi.RefundedAt,
                               BookImageUrl = b.ImageUrl
                           }).ToList();

            // Calculate totals
            ViewBag.TotalRefunds = refunds.Count;
            ViewBag.TotalRefundAmount = refunds.Sum(r => r.RefundAmount);

            return View(refunds);
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
