using Microsoft.AspNetCore.Mvc;
using OnlineLibrary.Infrastructure.Data;
using OnlineLibrary.Infrastructure.Domain.Entities;
using OnlineLibrary.Infrastructure.Security;
using OnlineLibrary.Web.Models;

namespace OnlineLibrary.Web.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // Admin Dashboard
        // =========================
        public IActionResult Dashboard()
        {
            // 1️⃣ Check login
            var userId = HttpContext.Session.GetString("UserId");
            var roleId = HttpContext.Session.GetString("RoleId");

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(roleId))
            {
                return RedirectToAction("Login", "Account");
            }

            // 2️⃣ Check role = Admin
            var roleName = _context.Roles
                .Where(r => r.RoleId == Guid.Parse(roleId))
                .Select(r => r.RoleName)
                .FirstOrDefault();

            if (roleName != "Admin")
            {
                return RedirectToAction("Index", "Home");
            }

            // 3️⃣ Dashboard statistics
            ViewBag.TotalUsers = _context.Users.Count();
            ViewBag.TotalBooks = _context.Books.Count();
            ViewBag.TotalCategories = _context.Categories.Count();
            ViewBag.ActiveMemberships = _context.Memberships.Count(m => m.IsActive);
            var librarianRoleId = _context.Roles
    .Where(r => r.RoleName == "Librarian")
    .Select(r => r.RoleId)
    .FirstOrDefault();

            ViewBag.ActiveLibrarians = _context.Users
                .Count(u => u.RoleId == librarianRoleId && u.IsActive);

            // =========================
            // MEMBERSHIP REQUEST COUNTS
            // =========================
            ViewBag.PendingMemberships = _context.Memberships
     .Count(m => m.Status == "Pending");

            ViewBag.ApprovedMemberships = _context.Memberships
                .Count(m => m.Status == "Approved");

            ViewBag.ActiveMemberships = _context.Memberships
    .Count(m => m.Status == "Approved");


            return View();
        }

        public IActionResult Memberships()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var memberships =
                from m in _context.Memberships
                join u in _context.Users
                    on m.UserId equals u.UserId
                orderby m.AppliedAt descending
                select new MembershipRequestViewModel
                {
                    MembershipId = m.MembershipId,
                    FullName = u.FullName,
                    Email = u.Email,
                    Status = m.Status,
                    AppliedAt = m.AppliedAt
                };

            return View(memberships.ToList());
        }

        [HttpPost]
        public IActionResult ApproveMembership(Guid id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var membership = _context.Memberships.Find(id);
            if (membership == null)
                return NotFound();

            membership.Status = "Approved";
            membership.ApprovedAt = DateTime.UtcNow;
            membership.StartDate = DateTime.Today;
            membership.ExpiryDate = DateTime.Today.AddYears(1);
            membership.IsActive = true;

            _context.SaveChanges();

            return RedirectToAction("Memberships");
        }

        [HttpPost]
        public IActionResult RejectMembership(Guid id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var membership = _context.Memberships.Find(id);
            if (membership == null)
                return NotFound();

            membership.Status = "Rejected";
            _context.SaveChanges();

            return RedirectToAction("Memberships");
        }


        // =========================
        //CREATE LIBRARIAN (GET)//
        // =========================
        public IActionResult CreateLibrarian()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            return View();
        }

        // =========================
        //CREATE LIBRARIAN (POST)//
        // =========================
        [HttpPost]
        public IActionResult CreateLibrarian(LibrarianCreateViewModel model)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            // Email uniqueness
            var emailExists = _context.Users.Any(u => u.Email == model.Email);
            if (emailExists)
            {
                ViewBag.Error = "Email already exists.";
                return View(model);
            }

            // Get Librarian role
            var librarianRole = _context.Roles.First(r => r.RoleName == "Librarian");

            var librarian = new User
            {
                UserId = Guid.NewGuid(),
                FullName = model.FullName,
                Email = model.Email,
                PasswordHash = PasswordHelper.HashPassword(model.Password),
                Phone = model.Phone,
                Address = model.Address,
                RoleId = librarianRole.RoleId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(librarian);
            _context.SaveChanges();

            return RedirectToAction("Librarians");
        }

        public IActionResult Librarians()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var librarianRoleId = _context.Roles
                .Where(r => r.RoleName == "Librarian")
                .Select(r => r.RoleId)
                .First();

            var librarians = _context.Users
                .Where(u => u.RoleId == librarianRoleId)
                .ToList();

            return View(librarians);
        }


        private bool IsAdmin()
        {
            var roleId = HttpContext.Session.GetString("RoleId");
            if (string.IsNullOrEmpty(roleId))
                return false;

            var roleName = _context.Roles
                .Where(r => r.RoleId == Guid.Parse(roleId))
                .Select(r => r.RoleName)
                .FirstOrDefault();

            return roleName == "Admin";
        }

    }
}
