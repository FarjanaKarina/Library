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
        public IActionResult Index(string search, Guid? categoryId)
        {
            var booksQuery =
                from b in _context.Books
                join c in _context.Categories
                    on b.CategoryId equals c.CategoryId
                select new PublicBookViewModel
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    Author = b.Author,
                    CategoryName = c.CategoryName,
                    ImageUrl = b.ImageUrl
                };

            // SEARCH
            if (!string.IsNullOrWhiteSpace(search))
            {
                booksQuery = booksQuery.Where(b =>
                    b.Title.Contains(search) ||
                    b.Author.Contains(search) ||
                    b.CategoryName.Contains(search));
            }

            // CATEGORY FILTER
            if (categoryId.HasValue)
            {
                booksQuery =
                    from b in booksQuery
                    join bk in _context.Books
                        on b.BookId equals bk.BookId
                    where bk.CategoryId == categoryId.Value
                    select b;
            }

            ViewBag.Categories = _context.Categories
                .OrderBy(c => c.OrderNo)
                .ToList();

            return View(booksQuery.ToList());
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
