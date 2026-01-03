using Microsoft.AspNetCore.Mvc;
using OnlineLibrary.Infrastructure.Data;
using OnlineLibrary.Infrastructure.Domain.Entities;
using OnlineLibrary.Infrastructure.Helpers;
using OnlineLibrary.Web.Models;

namespace OnlineLibrary.Web.Controllers
{
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StudentController(ApplicationDbContext context)
        {
            _context = context;
        }

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
            // MEMBERSHIP STATUS
            // =========================
            var membership = _context.Memberships
                .FirstOrDefault(m => m.UserId == userId);

            var membershipStatus = membership == null
                ? "Not Applied"
                : membership.Status;

            // =========================
            // BORROW HISTORY
            // =========================
            var borrowHistory =
                (from b in _context.BorrowTransactions
                 join bk in _context.Books on b.BookId equals bk.BookId
                 where b.UserId == userId
                 orderby b.BorrowDate descending
                 select new BorrowHistoryItem
                 {
                     BorrowId = b.BorrowId,
                     BookId = b.BookId,
                     BookTitle = bk.Title,
                     BorrowDate = b.BorrowDate,
                     DueDate = b.DueDate,
                     Status = b.IsReturned
                         ? "Returned"
                         : (b.DueDate < DateTime.Today ? "Overdue" : "Active"),
                     FineAmount = b.FineAmount
                 }).ToList();
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

                MembershipStatus = membershipStatus,
                ExpiryDate = membership?.ExpiryDate,      

                BorrowHistory = borrowHistory,
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
                join c in _context.Categories on b.CategoryId equals c.CategoryId
                select new StudentBookViewModel
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    Author = b.Author,
                    CategoryName = c.CategoryName,
                    ImageUrl = b.ImageUrl,
                    AvailableCopies = b.TotalCopies,
                    Price = b.Price
                };

            if (!string.IsNullOrWhiteSpace(search))
            {
                booksQuery = booksQuery.Where(b =>
                    b.Title!.Contains(search) ||
                    b.Author!.Contains(search) ||
                    b.CategoryName!.Contains(search));
            }

            return View(booksQuery.ToList());
        }

        // =========================
        // RETURN BOOK
        // =========================
        [HttpPost]
        public IActionResult ReturnBook(Guid borrowId)
        {
            if (!IsStudent())
                return RedirectToAction("Login", "Account");

            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Login", "Account");

            var userId = Guid.Parse(userIdStr);

            var borrow = _context.BorrowTransactions
                .FirstOrDefault(b => b.BorrowId == borrowId && b.UserId == userId);

            if (borrow == null || borrow.IsReturned)
                return RedirectToAction("Dashboard");

            borrow.ReturnDate = DateTime.Today;
            borrow.IsReturned = true;

            // =========================
            // FINE CALCULATION
            // =========================
            if (borrow.ReturnDate.Value > borrow.DueDate)
            {
                var overdueDays = (borrow.ReturnDate.Value - borrow.DueDate).Days;
                const decimal finePerDay = 10; // TK
                borrow.FineAmount = overdueDays * finePerDay;
            }
            else
            {
                borrow.FineAmount = 0;
            }

            // Restore book copies
            var book = _context.Books.Find(borrow.BookId);
            if (book != null)
            {
                book.TotalCopies += 1;
            }

            _context.SaveChanges();

            // =========================
            // NOTIFY LIBRARIANS
            // =========================
            var librarianIds = _context.Users
                .Where(u => _context.Roles.Any(r =>
                    r.RoleId == u.RoleId && r.RoleName == "Librarian"))
                .Select(u => u.UserId)
                .ToList();

            foreach (var lid in librarianIds)
            {
                NotificationHelper.Send(
                    _context,
                    lid,
                    "Book Returned",
                    "A book has been returned by a student.",
                    "success"
                );
            }

            return RedirectToAction("Dashboard");
        }

        // =========================
        // APPLY MEMBERSHIP (GET)
        // =========================
        public IActionResult ApplyMembership()
        {
            if (!IsStudent())
                return Unauthorized();

            var userId = Guid.Parse(HttpContext.Session.GetString("UserId"));

            // Prevent duplicate applications
            var exists = _context.Memberships.Any(m => m.UserId == userId);
            if (exists)
                return Content("<div class='p-3 text-danger'>You already have a membership request.</div>");

            return PartialView("_ApplyMembershipModal", new MembershipApplyViewModel());
        }

        // =========================
        // APPLY MEMBERSHIP (POST)
        // =========================
        [HttpPost]
        public IActionResult ApplyMembershipConfirm(int DurationMonths, string PaymentMethod, string TransactionId, decimal PaidAmount)
        {
            if (!IsStudent())
                return RedirectToAction("Login", "Account");

            var userId = Guid.Parse(HttpContext.Session.GetString("UserId"));

            // Prevent duplicate
            if (_context.Memberships.Any(m => m.UserId == userId))
                return RedirectToAction("Dashboard");

            var expectedAmount = DurationMonths switch
            {
                1 => 300,
                3 => 800,
                6 => 1500,
                _ => 0
            };

            if (PaidAmount != expectedAmount)
            {
                ModelState.AddModelError("", "Invalid payment amount.");
                return RedirectToAction("Dashboard");
            }

            var membership = new Membership
            {
                MembershipId = Guid.NewGuid(),
                UserId = userId,
                Status = "Pending",
                AppliedAt = DateTime.UtcNow,
                PaymentMethod = PaymentMethod,
                TransactionId = TransactionId,
                PaidAmount = PaidAmount,
                IsActive = false
            };

            _context.Memberships.Add(membership);
            _context.SaveChanges();

            // =========================
            // 🔔 NOTIFY ADMINS
            // =========================
            var adminIds = _context.Users
                    .Where(u => _context.Roles.Any(r =>
                        r.RoleId == u.RoleId && r.RoleName == "Admin"))
                    .Select(u => u.UserId)
                    .ToList();

            foreach (var adminId in adminIds)
            {
                NotificationHelper.Send(
                    _context,
                    adminId,
                    "Membership Request",
                    "A new membership request has been submitted.",
                    "info"
                );
            }

            // =========================
            // 🔔 NOTIFY LIBRARIANS
            // =========================
            var librarianIds = _context.Users
                .Where(u => _context.Roles.Any(r =>
                    r.RoleId == u.RoleId && r.RoleName == "Librarian"))
                .Select(u => u.UserId)
                .ToList();

            foreach (var lid in librarianIds)
            {
                NotificationHelper.Send(
                    _context,
                    lid,
                    "New Membership Request",
                    "A student has applied for membership.",
                    "info"
                );
            }

            return RedirectToAction("Dashboard");
        }

        // =========================
        // STUDENT NOTIFICATIONS
        // =========================
        public IActionResult Notifications()
        {
            if (!IsStudent())
                return RedirectToAction("Login", "Account");

            var userId = Guid.Parse(HttpContext.Session.GetString("UserId"));

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
