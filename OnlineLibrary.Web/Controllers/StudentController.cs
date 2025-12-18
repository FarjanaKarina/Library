using Microsoft.AspNetCore.Mvc;
using OnlineLibrary.Infrastructure.Data;
using OnlineLibrary.Infrastructure.Domain.Entities;
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
            // BORROW HISTORY (HERE)
            // =========================
            var borrowHistory =
    (from b in _context.BorrowTransactions
     join bk in _context.Books
         on b.BookId equals bk.BookId
     where b.UserId == userId
     orderby b.BorrowDate descending
     select new BorrowHistoryItem
     {
         BookTitle = bk.Title,
         BorrowDate = b.BorrowDate,
         DueDate = b.DueDate,
         Status = b.IsReturned
             ? "Returned"
             : (b.DueDate < DateTime.Today
                 ? "Overdue"
                 : "Active"),
         FineAmount = b.FineAmount
     }).ToList();


            // =========================
            // VIEW MODEL
            // =========================
            var model = new StudentDashboardViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                MembershipStatus = membershipStatus,
                BorrowHistory = borrowHistory
            };

            return View(model);
        }


        public IActionResult ApplyMembership()
        {
            if (!IsStudent())
                return RedirectToAction("Login", "Account");

            var userId = Guid.Parse(HttpContext.Session.GetString("UserId"));

            var exists = _context.Memberships.Any(m => m.UserId == userId);
            if (exists)
                return RedirectToAction("Dashboard");

            return View();
        }

        [HttpPost]
        public IActionResult ApplyMembershipConfirm()
        {
            if (!IsStudent())
                return RedirectToAction("Login", "Account");

            var userId = Guid.Parse(HttpContext.Session.GetString("UserId"));

            var membership = new Membership
            {
                MembershipId = Guid.NewGuid(),
                UserId = userId,
                Status = "Pending",
                AppliedAt = DateTime.UtcNow
            };

            _context.Memberships.Add(membership);
            _context.SaveChanges();

            return RedirectToAction("Dashboard");
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
