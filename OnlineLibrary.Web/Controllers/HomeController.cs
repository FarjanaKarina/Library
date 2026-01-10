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
            // BEST SELLING (BY ACTUAL SALES)
            // =========================
            var bestSellingData = _context.OrderItems
                .GroupBy(oi => oi.BookId)
                .Select(g => new { BookId = g.Key, Count = g.Sum(oi => oi.Quantity) })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();

            var bestSellingIds = bestSellingData.Select(x => x.BookId).ToList();

            var topRatedBooks = _context.Books
                .Where(b => bestSellingIds.Contains(b.BookId))
                .ToList()
                .OrderBy(b => bestSellingIds.IndexOf(b.BookId))
                .Select(b => new BestSellingBookViewModel
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    Author = b.Author,
                    Price = b.Price,
                    ImageUrl = b.ImageUrl
                })
                .ToList();

            // =========================
            // EXPLORE OUR COLLECTION
            // FIRST 8 ADDED BOOKS
            // =========================
            var exploreBooks = _context.Books
                .OrderByDescending(b => b.CreatedAt)
                .Take(8)
                .Select(b => new PublicBookViewModel
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    Author = b.Author, // Added Author for better info
                    Price = b.Price,
                    ImageUrl = b.ImageUrl,
                    AvailableCopies = b.TotalCopies
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
                    ImageUrl = "/images/librarian-placeholder.png" // Default, will be overwritten
                })
                .ToList();

            // Assign specific images as requested
            var libImages = new[] { "/images/sara.jpg", "/images/liba.jpg", "/images/janne.jpg" };
            for (int i = 0; i < librarians.Count && i < libImages.Length; i++)
            {
                librarians[i].ImageUrl = libImages[i];
            }

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
        // VIEW ALL BEST SELLERS (PUBLIC)
        // =========================
        public IActionResult BestSellers(string? search)
        {
            var bestSellingData = _context.OrderItems
                .GroupBy(oi => oi.BookId)
                .Select(g => new { BookId = g.Key, Count = g.Sum(oi => oi.Quantity) })
                .OrderByDescending(x => x.Count)
                .ToList();

            var bestSellingIds = bestSellingData.Select(x => x.BookId).ToList();

            var booksQuery = _context.Books
                .Where(b => bestSellingIds.Contains(b.BookId));

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                booksQuery = booksQuery.Where(b => b.Title.ToLower().Contains(s) || b.Author.ToLower().Contains(s));
            }

            var books = booksQuery.ToList()
                .OrderBy(b => bestSellingIds.IndexOf(b.BookId))
                .Select(b => new StudentBookViewModel
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    Author = b.Author,
                    ImageUrl = b.ImageUrl,
                    Price = b.Price,
                    AvailableCopies = b.TotalCopies
                })
                .ToList();

            ViewBag.Search = search;
            return View(books);
        }

        // =========================
        // PUBLIC BROWSE PAGE (DISCOVER MORE)
        // =========================
        public IActionResult Browse(string? search, Guid? categoryId)
        {
            var query = from b in _context.Books
                        select new StudentBookViewModel
                        {
                            BookId = b.BookId,
                            Title = b.Title,
                            Author = b.Author,
                            ImageUrl = b.ImageUrl,
                            Price = b.Price,
                            AvailableCopies = b.TotalCopies
                        };

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(b => b.Title.ToLower().Contains(s) || b.Author.ToLower().Contains(s));
            }

            if (categoryId.HasValue)
            {
                query = from b in query
                        join bc in _context.BookCategories on b.BookId equals bc.BookId
                        where bc.CategoryId == categoryId.Value
                        select b;
            }

            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.SelectedCategory = categoryId;
            ViewBag.Search = search;

            return View(query.ToList());
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

        public IActionResult Privacy() => View();
        public IActionResult Contact() => View();
        public IActionResult About() => View();

        [HttpPost]
        public async Task<IActionResult> Contact(ContactMessageViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fill in all fields correctly.";
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var contactMsg = new OnlineLibrary.Infrastructure.Domain.Entities.ContactMessage
            {
                MessageId = Guid.NewGuid(),
                Name = model.Name,
                Email = model.Email,
                Subject = model.Subject,
                Message = model.Message,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.ContactMessages.Add(contactMsg);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Your message has been sent successfully!";
            return RedirectToAction("Index", "Home", new { area = "" });
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
