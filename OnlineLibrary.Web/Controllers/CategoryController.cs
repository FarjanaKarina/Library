using Microsoft.AspNetCore.Mvc;
using OnlineLibrary.Infrastructure.Data;
using OnlineLibrary.Infrastructure.Domain.Entities;

namespace OnlineLibrary.Web.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // CATEGORY LIST
        // =========================
        public IActionResult Index()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var categories = _context.Categories
                .OrderBy(c => c.OrderNo)
                .ToList();

            return View(categories);
        }

        // =========================
        // CREATE (GET)
        // =========================
        public IActionResult Create()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var maxOrderNo = _context.Categories.Any()
                ? _context.Categories.Max(c => c.OrderNo)
                : 0;

            ViewBag.MaxOrderNo = maxOrderNo;

            return View();
        }


        // =========================
        // CREATE (POST)
        // =========================
        [HttpPost]
        public IActionResult Create(Category model)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            // Trim inputs
            var categoryName = model.CategoryName?.Trim();

            // 1️⃣ Check duplicate Category Name
            var nameExists = _context.Categories
                .Any(c => c.CategoryName.ToLower() == categoryName.ToLower());

            if (nameExists)
            {
                ViewBag.Error = "Category name already exists.";
                return View(model);
            }

            // 2️⃣ Check duplicate OrderNo
            var orderExists = _context.Categories
                .Any(c => c.OrderNo == model.OrderNo);

            if (orderExists)
            {
                ViewBag.Error = "Order number already exists. Please choose another.";
                return View(model);
            }

            // 3️⃣ Save
            model.CategoryId = Guid.NewGuid();
            model.CategoryName = categoryName;
            model.CreatedAt = DateTime.UtcNow;

            _context.Categories.Add(model);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }


        // =========================
        // EDIT (GET)
        // =========================
        public IActionResult Edit(Guid id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var category = _context.Categories.Find(id);
            if (category == null)
                return NotFound();

            var maxOrderNo = _context.Categories.Any()
                ? _context.Categories.Max(c => c.OrderNo)
                : 0;

            ViewBag.MaxOrderNo = maxOrderNo;

            return View(category);
        }


        // =========================
        // EDIT (POST)
        // =========================
        [HttpPost]
        public IActionResult Edit(Category model)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var categoryName = model.CategoryName?.Trim();

            // 1️⃣ Duplicate Category Name (exclude current)
            var nameExists = _context.Categories.Any(c =>
                c.CategoryId != model.CategoryId &&
                c.CategoryName.ToLower() == categoryName.ToLower());

            if (nameExists)
            {
                ViewBag.Error = "Category name already exists.";

                ViewBag.MaxOrderNo = _context.Categories.Any()
                    ? _context.Categories.Max(c => c.OrderNo)
                    : 0;

                return View(model);
            }

            // 2️⃣ Duplicate OrderNo (exclude current)
            var orderExists = _context.Categories.Any(c =>
                c.CategoryId != model.CategoryId &&
                c.OrderNo == model.OrderNo);

            if (orderExists)
            {
                ViewBag.Error = "Order number already exists. Please choose another.";

                ViewBag.MaxOrderNo = _context.Categories.Any()
                    ? _context.Categories.Max(c => c.OrderNo)
                    : 0;

                return View(model);
            }

            // 3️⃣ Update
            model.CategoryName = categoryName;
            _context.Categories.Update(model);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // DELETE
        // =========================
        [HttpPost]
        public IActionResult Delete(Guid id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var category = _context.Categories.Find(id);
            if (category == null)
                return NotFound();

            _context.Categories.Remove(category);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }


        // =========================
        // ADMIN CHECK
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
