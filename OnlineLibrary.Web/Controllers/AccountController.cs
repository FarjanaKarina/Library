using Microsoft.AspNetCore.Mvc;
using OnlineLibrary.Infrastructure.Data;
using OnlineLibrary.Infrastructure.Domain.Entities;
using OnlineLibrary.Infrastructure.Security;
using OnlineLibrary.Web.Models;

namespace OnlineLibrary.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }
        // =========================
        // GET: Registration
        // =========================
        public IActionResult Register()
        {
            return View();
        }
        // =========================
        // POST: Registration
        // =========================
        [HttpPost]
        public IActionResult Register(StudentRegisterViewModel model)
        {
            // Email uniqueness
            var emailExists = _context.Users.Any(u => u.Email == model.Email);
            if (emailExists)
            {
                ViewBag.Error = "Email already exists.";
                return View(model);
            }

            // Get Student role
            var studentRole = _context.Roles.First(r => r.RoleName == "Student");

            var user = new User
            {
                UserId = Guid.NewGuid(),
                FullName = model.FullName,
                Email = model.Email,
                PasswordHash = PasswordHelper.HashPassword(model.Password),
                Phone = model.Phone,
                Address = model.Address,
                RoleId = studentRole.RoleId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            // Redirect to login after successful registration
            return RedirectToAction("Login");
        }

        // =========================
        // GET: Login
        // =========================
        public IActionResult Login()
        {
            // If already logged in, redirect away
            if (HttpContext.Session.GetString("UserId") != null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // =========================
        // POST: Login
        // =========================
        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) ||
                string.IsNullOrWhiteSpace(model.Password))
            {
                ViewBag.Error = "Email and Password are required.";
                return View(model);
            }

            var hashedPassword = PasswordHelper.HashPassword(model.Password);

            var user = _context.Users
                .FirstOrDefault(u =>
                    u.Email == model.Email &&
                    u.PasswordHash == hashedPassword &&
                    u.IsActive);

            if (user == null)
            {
                ViewBag.Error = "Invalid email or password.";
                return View(model);
            }

            // Store session values
            HttpContext.Session.SetString("UserId", user.UserId.ToString());
            HttpContext.Session.SetString("RoleId", user.RoleId.ToString());

            return RedirectToDashboard(user.RoleId);
        }

        // =========================
        // Logout
        // =========================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // =========================
        // Role-based redirect
        // =========================
        private IActionResult RedirectToDashboard(Guid roleId)
        {
            var roleName = _context.Roles
                .Where(r => r.RoleId == roleId)
                .Select(r => r.RoleName)
                .FirstOrDefault();

            return roleName switch
            {
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                "Librarian" => RedirectToAction("Dashboard", "Librarian"),
                "Student" => RedirectToAction("Dashboard", "Student"),
                _ => RedirectToAction("Index", "Home")
            };
        }
    }
}
