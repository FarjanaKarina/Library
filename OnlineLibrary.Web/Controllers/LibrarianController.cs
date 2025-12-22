using Microsoft.AspNetCore.Mvc;
using OnlineLibrary.Infrastructure.Data;
using OnlineLibrary.Infrastructure.Domain.Entities;
using OnlineLibrary.Infrastructure.Helpers;
using OnlineLibrary.Web.Models;

namespace OnlineLibrary.Web.Controllers
{
    public class LibrarianController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LibrarianController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // LIBRARIAN DASHBOARD
        // =========================
        public IActionResult Dashboard()
        {
            if (!IsLibrarian())
                return RedirectToAction("Login", "Account");

            // =========================
            // SUMMARY STATS
            // =========================
            ViewBag.TotalBooks = _context.Books.Count();

            ViewBag.ActiveBorrows = _context.BorrowTransactions
                .Count(b => !b.IsReturned);

            ViewBag.OverdueBorrows = _context.BorrowTransactions
                .Count(b => !b.IsReturned && b.DueDate < DateTime.Today);

            ViewBag.ActiveMemberships = _context.Memberships
                .Count(m => m.IsActive);

            // =========================
            // OVERDUE LIST
            // =========================
            var overdueBorrows =
                from b in _context.BorrowTransactions
                join u in _context.Users on b.UserId equals u.UserId
                join bk in _context.Books on b.BookId equals bk.BookId
                where !b.IsReturned && b.DueDate < DateTime.Today
                orderby b.DueDate
                select new LibrarianOverdueViewModel
                {
                    BorrowId = b.BorrowId,
                    StudentName = u.FullName,
                    BookTitle = bk.Title,
                    DueDate = b.DueDate,
                    OverdueDays = (DateTime.Today - b.DueDate).Days,
                    FineAmount = b.FineAmount
                };

            // =========================
            // MEMBERSHIP REQUESTS
            // =========================
            var pendingMemberships =
                from m in _context.Memberships
                join u in _context.Users on m.UserId equals u.UserId
                where m.Status == "Pending"
                orderby m.AppliedAt
                select new LibrarianMembershipRequestViewModel
                {
                    MembershipId = m.MembershipId,
                    StudentName = u.FullName,
                    Email = u.Email,
                    AppliedAt = m.AppliedAt
                };

            var model = new LibrarianDashboardViewModel
            {
                OverdueBorrows = overdueBorrows.ToList(),
                PendingMemberships = pendingMemberships.ToList()
            };

            return View(model);
        }


        // =========================
        // OVERDUE BORROWS
        // =========================
        public IActionResult Overdue()
        {
            if (!IsLibrarian())
                return RedirectToAction("Login", "Account");

            var overdue =
                from b in _context.BorrowTransactions
                join u in _context.Users on b.UserId equals u.UserId
                join bk in _context.Books on b.BookId equals bk.BookId
                where !b.IsReturned && b.DueDate < DateTime.Today
                select new
                {
                    BorrowId = b.BorrowId,
                    UserId = u.UserId,
                    StudentName = u.FullName,
                    BookTitle = bk.Title,
                    DueDate = b.DueDate,
                    OverdueDays = (DateTime.Today - b.DueDate).Days,
                    Fine = b.FineAmount
                };

            return View(overdue.ToList());
        }

        // =========================
        // FINE VERIFICATION LIST
        // =========================
        public IActionResult VerifyFines()
        {
            if (!IsLibrarian())
                return RedirectToAction("Login", "Account");

            var fines =
                from b in _context.BorrowTransactions
                join u in _context.Users on b.UserId equals u.UserId
                join bk in _context.Books on b.BookId equals bk.BookId
                where b.FineAmount > 0 && !b.IsFinePaid
                orderby b.DueDate
                select new
                {
                    BorrowId = b.BorrowId,
                    UserId = u.UserId,
                    UserName = u.FullName,
                    BookTitle = bk.Title,
                    FineAmount = b.FineAmount,
                    DueDate = b.DueDate
                };

            return View(fines.ToList());
        }


        // =========================
        // CONFIRM FINE PAYMENT
        // =========================
        [HttpPost]
        public IActionResult ConfirmFine(Guid borrowId)
        {
            if (!IsLibrarian())
                return RedirectToAction("Login", "Account");

            var borrow = _context.BorrowTransactions.Find(borrowId);
            if (borrow == null)
                return NotFound();

            borrow.IsFinePaid = true;
            _context.SaveChanges();

            // 🔔 NOTIFY USER
            NotificationHelper.Send(
                _context,
                borrow.UserId,
                "Fine Cleared",
                "Your fine has been verified and cleared by the librarian.",
                "success");

            return RedirectToAction("VerifyFines");
        }


        // =========================
        // RETURN BOOK
        // =========================
        [HttpPost]
        public IActionResult Return(Guid borrowId)
        {
            if (!IsLibrarian())
                return RedirectToAction("Login", "Account");

            var borrow = _context.BorrowTransactions.Find(borrowId);
            if (borrow == null)
                return NotFound();

            if (borrow.IsReturned)
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
            return RedirectToAction("Dashboard");
        }

        // =========================
        // SEND OVERDUE WARNING
        // =========================
        [HttpPost]
        public IActionResult SendOverdueWarning(Guid userId, string bookTitle)
        {
            if (!IsLibrarian())
                return RedirectToAction("Login", "Account");

            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = userId,
                Title = "Overdue Book",
                Message = $"Your borrowed book '{bookTitle}' is overdue. Please return it immediately.",
                Type = "overdue",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            _context.SaveChanges();

            return RedirectToAction("Dashboard");
        }

        // =========================
        // ROLE CHECK
        // =========================
        private bool IsLibrarian()
        {
            var roleId = HttpContext.Session.GetString("RoleId");
            if (string.IsNullOrEmpty(roleId))
                return false;

            var roleName = _context.Roles
                .Where(r => r.RoleId == Guid.Parse(roleId))
                .Select(r => r.RoleName)
                .FirstOrDefault();

            return roleName == "Librarian";
        }
    }
}
