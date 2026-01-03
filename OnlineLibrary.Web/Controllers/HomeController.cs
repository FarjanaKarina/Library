using Microsoft.AspNetCore.Mvc;
using OnlineLibrary.Infrastructure.Data;
using OnlineLibrary.Web.Models;
using System.Diagnostics;

namespace OnlineLibrary.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ApplicationDbContext context,
            ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // =========================
        // PUBLIC LANDING PAGE
        // =========================
        public IActionResult Index(string? search)
        {
            // =========================
            // CURRENT USER ROLE
            // =========================
            ViewBag.CurrentRole = "";

            var roleId = HttpContext.Session.GetString("RoleId");
            if (!string.IsNullOrEmpty(roleId))
            {
                ViewBag.CurrentRole = _context.Roles
                    .Where(r => r.RoleId == Guid.Parse(roleId))
                    .Select(r => r.RoleName)
                    .FirstOrDefault();
            }

            // =========================
            // FEATURED BOOKS
            // =========================
            ViewBag.FeaturedBooks = _context.Books
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .Select(b => new FeaturedBookViewModel
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    Description = b.Description.Length > 150
                        ? b.Description.Substring(0, 150) + "..."
                        : b.Description,
                    ImageUrl = b.ImageUrl,
                    PdfUrl = b.PdfUrl
                })
                .ToList();

            // =========================
            // BOOK LIST + CATEGORIES
            // =========================
            var booksQuery =
                from b in _context.Books
                select new PublicBookViewModel
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    Author = b.Author,
                    Publisher = b.Publisher,
                    ImageUrl = b.ImageUrl,
                    Categories =
                        (from bc in _context.BookCategories
                         join c in _context.Categories
                             on bc.CategoryId equals c.CategoryId
                         where bc.BookId == b.BookId
                         select c.CategoryName).ToList()
                };

            // =========================
            // SEARCH (TITLE / AUTHOR / PUBLISHER / CATEGORY)
            // =========================
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();

                booksQuery = booksQuery.Where(b =>
                    (b.Title != null && b.Title.ToLower().Contains(s)) ||
                    (b.Author != null && b.Author.ToLower().Contains(s)) ||
                    (b.Publisher != null && b.Publisher.ToLower().Contains(s)) ||
                    b.Categories.Any(c => c.ToLower().Contains(s))
                );
            }

            ViewBag.Search = search;

            return View(booksQuery.ToList());
        }
        [HttpGet]
        public IActionResult Search(string? search)
        {
            var query =
                from b in _context.Books
                select new PublicBookViewModel
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    Author = b.Author,
                    Publisher = b.Publisher,
                    ImageUrl = b.ImageUrl,
                    Categories =
                        (from bc in _context.BookCategories
                         join c in _context.Categories
                             on bc.CategoryId equals c.CategoryId
                         where bc.BookId == b.BookId
                         select c.CategoryName).ToList()
                };

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();

                query = query.Where(b =>
                    (b.Title != null && b.Title.ToLower().Contains(s)) ||
                    (b.Author != null && b.Author.ToLower().Contains(s)) ||
                    (b.Publisher != null && b.Publisher.ToLower().Contains(s)) ||
                    b.Categories.Any(c => c.ToLower().Contains(s))
                );
            }

            return Json(query.Take(20).ToList());
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
