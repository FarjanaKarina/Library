using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OnlineLibrary.Infrastructure.Data;
using OnlineLibrary.Infrastructure.Domain.Entities;
using OnlineLibrary.Infrastructure.Helpers;
using OnlineLibrary.Web.Models;

namespace OnlineLibrary.Web.Controllers
{
    public class BookController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public BookController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // =========================
        // BOOK LIST (ADMIN)
        // =========================
        public IActionResult Index()
        {
            if (!IsAdminOrLibrarian())
                return RedirectToAction("Login", "Account");

            var books = (
                from b in _context.Books
                join c in _context.Categories
                    on b.CategoryId equals c.CategoryId
                orderby b.CreatedAt descending
                select new BookListViewModel
                {
                    BookId = b.BookId,
                    Title = b.Title,
                    Author = b.Author,
                    Price = b.Price,
                    TotalCopies = b.TotalCopies,
                    PurchaseDate = b.PurchaseDate,
                    ImageUrl = b.ImageUrl,
                    CategoryName = c.CategoryName
                }
            ).ToList();

            return View(books);
        }
        // =========================
        // DETAILS 
        // =========================
        public IActionResult Details(Guid id)
        {
            // 🔐 LOGIN REQUIRED
            if (HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var book =
                (from b in _context.Books
                 join c in _context.Categories
                     on b.CategoryId equals c.CategoryId
                 where b.BookId == id
                 select new OnlineLibrary.Web.Models.BookDetailsViewModel
                 {
                     BookId = b.BookId,
                     Title = b.Title,
                     Author = b.Author,
                     Publisher = b.Publisher,
                     ISBN = b.ISBN,
                     Description = b.Description,
                     CategoryName = c.CategoryName,
                     PublishDate = b.PublishDate,
                     Price = b.Price,
                     TotalCopies = b.TotalCopies,
                     ImageUrl = b.ImageUrl
                 }).FirstOrDefault();

            if (book == null)
                return NotFound();

            return View(book);
        }


        // =========================
        // CREATE (GET)
        // =========================
        public IActionResult Create()
        {
            if (!IsAdminOrLibrarian())
                return RedirectToAction("Login", "Account");

            LoadCategories();
            return View();
        }

        // =========================
        // CREATE (POST)
        // =========================
        [HttpPost]
        public IActionResult Create(Book model, IFormFile imageFile)
        {
            if (!IsAdminOrLibrarian())
                return RedirectToAction("Login", "Account");

            LoadCategories(model.CategoryId);

            // -------------------------
            // VALIDATIONS
            // -------------------------

            if (model.CategoryId == Guid.Empty)
            {
                ViewBag.Error = "Please select a category.";
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.Description))
            {
                ViewBag.Error = "Description is required.";
                return View(model);
            }

            model.PurchaseDate = DateTime.Today;

            if (model.PublishDate > model.PurchaseDate)
            {
                ViewBag.Error = "Publish date cannot be after purchase date.";
                return View(model);
            }

            if (model.Price < 0)
            {
                ViewBag.Error = "Price cannot be negative.";
                return View(model);
            }

            if (model.TotalCopies < 0)
            {
                ViewBag.Error = "Total copies cannot be negative.";
                return View(model);
            }

            // ISBN validation
            if (!string.IsNullOrWhiteSpace(model.ISBN))
            {
                if (model.ISBN.Length != 13)
                {
                    ViewBag.Error = "ISBN must be exactly 13 characters.";
                    return View(model);
                }

                var isbnExists = _context.Books.Any(b => b.ISBN == model.ISBN);
                if (isbnExists)
                {
                    ViewBag.Error = "ISBN already exists.";
                    return View(model);
                }
            }

            // -------------------------
            // IMAGE UPLOAD
            // -------------------------
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsPath = Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "books"
                );

                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                var fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    imageFile.CopyTo(stream);
                }

                model.ImageUrl = "/uploads/books/" + fileName;
            }

            // -------------------------
            // SAVE BOOK
            // -------------------------
            model.BookId = Guid.NewGuid();
            model.CreatedAt = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(model.ISBN))
            {
                model.ISBN = Guid.NewGuid().ToString("N").Substring(0, 13);
            }

            _context.Books.Add(model);
            _context.SaveChanges();

            // =========================
            // AUDIT LOG: BOOK ADDED
            // =========================
            _context.AuditLogs.Add(new AuditLog
            {
                AuditLogId = Guid.NewGuid(),
                ActorUserId = Guid.Parse(HttpContext.Session.GetString("UserId")),
                ActorRole = GetCurrentRole(), // Admin or Librarian
                Action = "Book Added",
                EntityName = "Book",
                EntityId = model.BookId,
                Description = $"Book '{model.Title}' was added."
            });

            _context.SaveChanges();


            NotificationHelper.Broadcast(
    _context,
    "New Book Added",
    $"'{model.Title}' is now available in the library.",
    "system");

            return RedirectToAction(nameof(Index));
        }

        private string GetCurrentRole()
        {
            var roleId = HttpContext.Session.GetString("RoleId");
            if (string.IsNullOrEmpty(roleId))
                return "Unknown";

            return _context.Roles
                .Where(r => r.RoleId == Guid.Parse(roleId))
                .Select(r => r.RoleName)
                .FirstOrDefault() ?? "Unknown";
        }

        // =========================
        // EDIT (GET)
        // =========================
        public IActionResult Edit(Guid id)
        {
            if (!IsAdminOrLibrarian())
                return RedirectToAction("Login", "Account");

            var book = _context.Books.Find(id);
            if (book == null)
                return NotFound();

            LoadCategories(book.CategoryId);
            return View(book);
        }

        // =========================
        // EDIT (POST)
        // =========================
        [HttpPost]
        public IActionResult Edit(Book model, IFormFile imageFile)
        {
            if (!IsAdminOrLibrarian())
                return RedirectToAction("Login", "Account");

            LoadCategories(model.CategoryId);

            if (model.CategoryId == Guid.Empty)
            {
                ViewBag.Error = "Please select a category.";
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.Description))
            {
                ViewBag.Error = "Description is required.";
                return View(model);
            }

            if (imageFile == null || imageFile.Length == 0)
            {
                ViewBag.Error = "Book image is required.";
                return View(model);
            }

            model.PurchaseDate = DateTime.Today;

            if (model.PublishDate > model.PurchaseDate)
            {
                ViewBag.Error = "Publish date cannot be after purchase date.";
                return View(model);
            }

            if (model.Price < 0)
            {
                ViewBag.Error = "Price cannot be negative.";
                return View(model);
            }

            if (model.TotalCopies < 0)
            {
                ViewBag.Error = "Total copies cannot be negative.";
                return View(model);
            }

            // ISBN validation (exclude current)
            if (!string.IsNullOrWhiteSpace(model.ISBN))
            {
                if (model.ISBN.Length != 13)
                {
                    ViewBag.Error = "ISBN must be exactly 13 characters.";
                    return View(model);
                }

                var isbnExists = _context.Books.Any(b =>
                    b.ISBN == model.ISBN && b.BookId != model.BookId);

                if (isbnExists)
                {
                    ViewBag.Error = "ISBN already exists.";
                    return View(model);
                }
            }

            var existingBook = _context.Books.Find(model.BookId);
            if (existingBook == null)
                return NotFound();

            // -------------------------
            // IMAGE UPDATE (OPTIONAL)
            // -------------------------
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsPath = Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "books"
                );

                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                var fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    imageFile.CopyTo(stream);
                }

                if (!string.IsNullOrEmpty(existingBook.ImageUrl))
                {
                    var oldPath = Path.Combine(
                        _environment.WebRootPath,
                        existingBook.ImageUrl.TrimStart('/')
                    );

                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                existingBook.ImageUrl = "/uploads/books/" + fileName;
            }

            // -------------------------
            // UPDATE FIELDS
            // -------------------------
            existingBook.Title = model.Title;
            existingBook.Author = model.Author;
            existingBook.Publisher = model.Publisher;
            existingBook.Description = model.Description;
            existingBook.CategoryId = model.CategoryId;
            existingBook.PublishDate = model.PublishDate;
            existingBook.Price = model.Price;
            existingBook.TotalCopies = model.TotalCopies;
            existingBook.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(model.ISBN))
            {
                existingBook.ISBN = model.ISBN;
            }

            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // DELETE
        // =========================
        [HttpPost]
        public IActionResult Delete(Guid id)
        {
            if (!IsAdminOrLibrarian())
                return RedirectToAction("Login", "Account");

            var book = _context.Books.Find(id);
            if (book == null)
                return NotFound();

            if (!string.IsNullOrEmpty(book.ImageUrl))
            {
                var imagePath = Path.Combine(
                    _environment.WebRootPath,
                    book.ImageUrl.TrimStart('/')
                );

                if (System.IO.File.Exists(imagePath))
                    System.IO.File.Delete(imagePath);
            }

            _context.Books.Remove(book);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // HELPERS
        // =========================
        private void LoadCategories(Guid? selectedId = null)
        {
            ViewBag.Categories = new SelectList(
                _context.Categories.OrderBy(c => c.OrderNo),
                "CategoryId",
                "CategoryName",
                selectedId
            );
        }

        private bool IsAdminOrLibrarian()
        {
            var roleId = HttpContext.Session.GetString("RoleId");
            if (string.IsNullOrEmpty(roleId))
                return false;

            var roleName = _context.Roles
                .Where(r => r.RoleId == Guid.Parse(roleId))
                .Select(r => r.RoleName)
                .FirstOrDefault();

            return roleName == "Admin" || roleName == "Librarian";
        }


    }
}
