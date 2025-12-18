using Microsoft.AspNetCore.Mvc;
using OnlineLibrary.Infrastructure.Data;

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

            var borrows = _context.BorrowTransactions
                .Where(b => !b.IsReturned)
                .ToList();

            return View(borrows);
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
                var overdueDays =
                    (borrow.ReturnDate.Value - borrow.DueDate).Days;

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
                book.TotalCopies += 1;

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
