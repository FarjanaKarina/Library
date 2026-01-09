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
            // FEATURED BOOKS (CAROUSEL)
            // =========================
            var featuredBooks = _context.Books
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
            // BEST SELLING / TOP RATED
            // =========================
            var topRatedBooks = _context.Books
                .Where(b => b.Rating >= 4.5)
                .OrderByDescending(b => b.Rating)
                .Take(10)
                .Select(b => new BestSellingBookViewModel
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    Author = b.Author,
                    Price = b.Price,
                    ImageUrl = b.ImageUrl
                })
                .ToList();

            // Fallback if no ratings yet
            if (!topRatedBooks.Any())
            {
                topRatedBooks = _context.Books
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(10)
                    .Select(b => new BestSellingBookViewModel
                    {
                        BookId = b.BookId,
                        Title = b.Title,
                        Author = b.Author,
                        Price = b.Price,
                        ImageUrl = b.ImageUrl
                    })
                    .ToList();
            }

            // =========================
            // EXPLORE OUR COLLECTION
            // FIRST 8 ADDED BOOKS
            // =========================
            var exploreBooks = _context.Books
                .OrderBy(b => b.CreatedAt)
                .Take(8)
                .Select(b => new PublicBookViewModel
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    Price = b.Price,
                    ImageUrl = b.ImageUrl
                })
                .ToList();

            // =========================
            // MAIN SEARCHABLE BOOK LIST
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

            // =========================
            // CATEGORIES (SECTION 5)
            // =========================
            var categories = _context.Categories
                .Take(6)
                .Select(c => new CategoryViewModel
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName ?? "Unknown",
                    Icon = (c.CategoryName != null && c.CategoryName.Contains("Fiction")) ? "bi-rocket-takeoff" :
                           (c.CategoryName != null && c.CategoryName.Contains("Science")) ? "bi-cpu" :
                           (c.CategoryName != null && c.CategoryName.Contains("History")) ? "bi-bank" :
                           (c.CategoryName != null && c.CategoryName.Contains("Arts")) ? "bi-palette" :
                           (c.CategoryName != null && c.CategoryName.Contains("Children")) ? "bi-balloon" : "bi-bookmark-star"
                })
                .ToList();

            // =========================
            // LIBRARIANS (SECTION 7)
            // =========================
            var librarianRoleId = _context.Roles
                .Where(r => r.RoleName == "Librarian")
                .Select(r => r.RoleId)
                .FirstOrDefault();

            var librarians = _context.Users
                .Where(u => u.RoleId == librarianRoleId && u.IsActive)
                .Take(3)
                .Select(u => new LibrarianViewModel
                {
                    FullName = u.FullName,
                    Role = "Librarian",
                    ImageUrl = "/images/librarian-placeholder.png"
                })
                .ToList();

            // =========================
            // FINAL VIEW MODEL
            // =========================
            var model = new HomeIndexViewModel
            {
                FeaturedBooks = featuredBooks,
                TopRatedBooks = topRatedBooks,
                ExploreBooks = exploreBooks,
                AllBooks = booksQuery.ToList(),
                Categories = categories,
                Librarians = librarians
            };

            return View(model);
        }

        // =========================
        // AJAX SEARCH (UNCHANGED)
        // =========================
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

        public IActionResult Privacy() => View();
        public IActionResult Contact() => View();
        public IActionResult About() => View();

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
