using Microsoft.AspNetCore.Mvc;
using OnlineLibrary.Infrastructure.Data;
using OnlineLibrary.Infrastructure.Domain.Entities;

namespace OnlineLibrary.Web.Controllers
{
    public class BorrowController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BorrowController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // BORROW BOOK (STUDENT)
        // =========================
        public IActionResult Create(Guid bookId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Login", "Account");

            var userId = Guid.Parse(userIdStr);

            // Membership check
            var membership = _context.Memberships
                .FirstOrDefault(m =>
                    m.UserId == userId &&
                    m.Status == "Approved" &&
                    m.IsActive);

            if (membership == null)
                return Content("Membership not approved.");

            var book = _context.Books.Find(bookId);
            if (book == null || book.TotalCopies <= 0)
                return Content("Book not available.");

            var borrow = new BorrowTransaction
            {
                BorrowId = Guid.NewGuid(),
                UserId = userId,
                BookId = bookId,
                BorrowDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(7),
                IsReturned = false
            };

            book.TotalCopies -= 1;

            _context.BorrowTransactions.Add(borrow);
            _context.SaveChanges();

            return RedirectToAction("Dashboard", "Student");
        }
    }
}
