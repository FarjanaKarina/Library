using Microsoft.AspNetCore.Mvc;
using OnlineLibrary.Infrastructure.Data;
using OnlineLibrary.Infrastructure.Domain.Entities;
using OnlineLibrary.Infrastructure.Helpers;
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

            var librarianRoleId = _context.Roles
                .Where(r => r.RoleName == "Librarian")
                .Select(r => r.RoleId)
                .FirstOrDefault();

            ViewBag.ActiveLibrarians = _context.Users
                .Count(u => u.RoleId == librarianRoleId && u.IsActive);

            return View();
        }

        [HttpPost]
        public IActionResult SendAnnouncement(string title, string message)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            NotificationHelper.Broadcast(
                _context,
                title,
                message,
                "system"
            );

            return RedirectToAction("Dashboard");
        }


       
        // =========================
        // CREATE LIBRARIAN (GET)
        // =========================
        public IActionResult CreateLibrarian()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            return View();
        }

        // =========================
        // CREATE LIBRARIAN (POST)
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

        // =========================
        // List Librarians
        // =========================
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
        // =========================
        // AUDIT LOGS (READ ONLY)
        // =========================
        public IActionResult AuditLogs()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var logs =
                from a in _context.AuditLogs
                join u in _context.Users
                    on a.ActorUserId equals u.UserId
                orderby a.CreatedAt descending
                select new AdminAuditLogViewModel
                {
                    ActorName = u.FullName,
                    ActorRole = a.ActorRole,
                    Action = a.Action,
                    EntityName = a.EntityName,
                    Description = a.Description,
                    CreatedAt = a.CreatedAt
                };

            return View(logs.ToList());
        }


        // =========================
        // Helper: Is Admin
        // =========================
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
