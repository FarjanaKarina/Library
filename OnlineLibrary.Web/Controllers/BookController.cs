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

            var books =
                (from b in _context.Books
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

                     // ✅ MULTIPLE CATEGORIES
                     Categories = (
                         from bc in _context.BookCategories
                         join c in _context.Categories
                             on bc.CategoryId equals c.CategoryId
                         where bc.BookId == b.BookId
                         select c.CategoryName
                     ).ToList()
                 }).ToList();

            ViewBag.CurrentRole = _context.Roles
    .Where(r => r.RoleId == Guid.Parse(HttpContext.Session.GetString("RoleId")))
    .Select(r => r.RoleName)
    .FirstOrDefault();


            return View(books);
        }

        // =========================
        // DETAILS 
        // =========================
        public IActionResult Details(Guid id)
        {
            // =========================
            // USER CONTEXT (OPTIONAL)
            // =========================
            var userIdStr = HttpContext.Session.GetString("UserId");

            ViewBag.IsLoggedIn = !string.IsNullOrEmpty(userIdStr);
            ViewBag.CurrentRole = null;
            ViewBag.IsMember = false;

            if (!string.IsNullOrEmpty(userIdStr))
            {
                var roleIdStr = HttpContext.Session.GetString("RoleId");

                if (!string.IsNullOrEmpty(roleIdStr))
                {
                    var roleId = Guid.Parse(roleIdStr);

                    ViewBag.CurrentRole = _context.Roles
                        .Where(r => r.RoleId == roleId)
                        .Select(r => r.RoleName)
                        .FirstOrDefault();
                }

                var userId = Guid.Parse(userIdStr);

                ViewBag.IsMember = _context.Memberships
    .Any(m => m.UserId == userId
           && m.Status == "Approved");

            }

            // =========================
            // BOOK DETAILS
            // =========================
            var book =
                (from b in _context.Books
                 where b.BookId == id
                 select new BookDetailsViewModel
                 {
                     BookId = b.BookId,
                     Title = b.Title,
                     Author = b.Author,
                     Publisher = b.Publisher,
                     ISBN = b.ISBN,
                     Description = b.Description,
                     PublishDate = b.PublishDate,
                     Price = b.Price,
                     TotalCopies = b.TotalCopies,
                     ImageUrl = b.ImageUrl,
                     PdfUrl = b.PdfUrl,

                     // ✅ MULTIPLE CATEGORIES
                     Categories = (
                         from bc in _context.BookCategories
                         join c in _context.Categories
                             on bc.CategoryId equals c.CategoryId
                         where bc.BookId == b.BookId
                         select c.CategoryName
                     ).ToList()
                 }).FirstOrDefault();

            if (book == null)
                return NotFound();

            return View(book);
        }
        public IActionResult ReadPdf(Guid id)
        {
            // =========================
            // SESSION AUTH CHECK
            // =========================
            var userIdStr = HttpContext.Session.GetString("UserId");
            var roleIdStr = HttpContext.Session.GetString("RoleId");

            if (string.IsNullOrEmpty(userIdStr) || string.IsNullOrEmpty(roleIdStr))
                return Unauthorized();

            var userId = Guid.Parse(userIdStr);
            var roleId = Guid.Parse(roleIdStr);

            var roleName = _context.Roles
                .Where(r => r.RoleId == roleId)
                .Select(r => r.RoleName)
                .FirstOrDefault();

            if (roleName != "Student" &&
                roleName != "Admin" &&
                roleName != "Librarian")
                return Unauthorized();

            // =========================
            // GET BOOK
            // =========================
            var book = _context.Books.Find(id);

            if (book == null || string.IsNullOrEmpty(book.PdfUrl))
                return NotFound();

            var filePath = Path.Combine(
                _environment.WebRootPath,
                book.PdfUrl.TrimStart('/')
            );

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            // =========================
            // CONTINUE READING (STUDENT)
            // =========================
            if (roleName == "Student")
            {
                var wishlist = _context.Wishlists
                    .FirstOrDefault(w => w.UserId == userId && w.BookId == id);

                // If not already wishlisted, auto-add
                if (wishlist == null)
                {
                    wishlist = new Wishlist
                    {
                        WishlistId = Guid.NewGuid(),
                        UserId = userId,
                        BookId = id,
                        CreatedAt = DateTime.UtcNow,
                        LastReadAt = DateTime.UtcNow
                    };

                    _context.Wishlists.Add(wishlist);
                }
                else
                {
                    wishlist.LastReadAt = DateTime.UtcNow;
                }

                _context.SaveChanges();
            }

            // =========================
            // STREAM PDF (INLINE)
            // =========================
            return PhysicalFile(filePath, "application/pdf");
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
        public IActionResult Create(Book model, List<Guid> CategoryIds, IFormFile imageFile, IFormFile pdfFile)
        {
            if (!IsAdminOrLibrarian())
                return RedirectToAction("Login", "Account");

            // =========================
            // VALIDATIONS
            // =========================
            if (CategoryIds == null || !CategoryIds.Any())
            {
                ViewBag.Error = "Please select at least one category.";
                ViewBag.SelectedCategoryIds = CategoryIds;
                LoadCategories();
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.Description))
            {
                ViewBag.Error = "Description is required.";
                ViewBag.SelectedCategoryIds = CategoryIds;
                LoadCategories();
                return View(model);
            }

            model.PurchaseDate = DateTime.Today;

            if (model.PublishDate > model.PurchaseDate)
            {
                ViewBag.Error = "Publish date cannot be after purchase date.";
                ViewBag.SelectedCategoryIds = CategoryIds;
                LoadCategories();
                return View(model);
            }

            if (model.Price < 0 || model.TotalCopies < 0)
            {
                ViewBag.Error = "Price and copies cannot be negative.";
                ViewBag.SelectedCategoryIds = CategoryIds;
                LoadCategories();
                return View(model);
            }

            // ISBN validation
            if (!string.IsNullOrWhiteSpace(model.ISBN))
            {
                if (model.ISBN.Length != 13)
                {
                    ViewBag.Error = "ISBN must be exactly 13 characters.";
                    ViewBag.SelectedCategoryIds = CategoryIds;
                    LoadCategories();
                    return View(model);
                }

                var isbnExists = _context.Books.Any(b => b.ISBN == model.ISBN);
                if (isbnExists)
                {
                    ViewBag.Error = "ISBN already exists.";
                    ViewBag.SelectedCategoryIds = CategoryIds;
                    LoadCategories();
                    return View(model);
                }
            }

            // =========================
            // IMAGE UPLOAD
            // =========================
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "books");
                Directory.CreateDirectory(uploadsPath);

                var fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                var filePath = Path.Combine(uploadsPath, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                imageFile.CopyTo(stream);

                model.ImageUrl = "/uploads/books/" + fileName;
            }
            // =========================
            // PDF UPLOAD
            // =========================
            if (pdfFile != null && pdfFile.Length > 0)
            {
                var pdfPath = Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "books",
                    "pdf"
                );

                Directory.CreateDirectory(pdfPath);

                var pdfName = Guid.NewGuid() + ".pdf";
                var fullPath = Path.Combine(pdfPath, pdfName);

                using var stream = new FileStream(fullPath, FileMode.Create);
                pdfFile.CopyTo(stream);

                model.PdfUrl = "/uploads/books/pdf/" + pdfName;
            }
            // =========================
            // SAVE BOOK
            // =========================
            model.BookId = Guid.NewGuid();
            model.CreatedAt = DateTime.UtcNow;

            if (string.IsNullOrWhiteSpace(model.ISBN))
                model.ISBN = Guid.NewGuid().ToString("N").Substring(0, 13);

            _context.Books.Add(model);
            _context.SaveChanges();

            // =========================
            // SAVE CATEGORY MAPPINGS
            // =========================
            foreach (var categoryId in CategoryIds)
            {
                _context.BookCategories.Add(new BookCategory
                {
                    BookCategoryId = Guid.NewGuid(),
                    BookId = model.BookId,
                    CategoryId = categoryId
                });
            }

            _context.SaveChanges();

            // =========================
            // AUDIT LOG
            // =========================
            _context.AuditLogs.Add(new AuditLog
            {
                AuditLogId = Guid.NewGuid(),
                ActorUserId = Guid.Parse(HttpContext.Session.GetString("UserId")),
                ActorRole = GetCurrentRole(),
                Action = "Book Added",
                EntityName = "Book",
                EntityId = model.BookId,
                Description = $"Book '{model.Title}' was added."
            });

            _context.SaveChanges();

            // =========================
            // NOTIFICATION
            // =========================
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

            LoadCategories();

            ViewBag.SelectedCategoryIds = _context.BookCategories
                .Where(bc => bc.BookId == id)
                .Select(bc => bc.CategoryId)
                .ToList();

            return View(book);
        }

        [HttpPost]
        public IActionResult Edit(Book model, List<Guid> CategoryIds, IFormFile imageFile, IFormFile pdfFile)
        {
            if (!IsAdminOrLibrarian())
                return RedirectToAction("Login", "Account");

            // =========================
            // VALIDATION
            // =========================
            if (CategoryIds == null || !CategoryIds.Any())
            {
                ViewBag.Error = "Please select at least one category.";
                ViewBag.SelectedCategoryIds = CategoryIds;
                LoadCategories();
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.Description))
            {
                ViewBag.Error = "Description is required.";
                ViewBag.SelectedCategoryIds = CategoryIds;
                LoadCategories();
                return View(model);
            }

            model.PurchaseDate = DateTime.Today;

            if (model.PublishDate > model.PurchaseDate)
            {
                ViewBag.Error = "Publish date cannot be after purchase date.";
                ViewBag.SelectedCategoryIds = CategoryIds;
                LoadCategories();
                return View(model);
            }

            if (model.Price < 0 || model.TotalCopies < 0)
            {
                ViewBag.Error = "Price and copies cannot be negative.";
                ViewBag.SelectedCategoryIds = CategoryIds;
                LoadCategories();
                return View(model);
            }

            var existingBook = _context.Books.Find(model.BookId);
            if (existingBook == null)
                return NotFound();

            // =========================
            // IMAGE UPDATE
            // =========================
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "books");
                Directory.CreateDirectory(uploadsPath);

                var fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                var filePath = Path.Combine(uploadsPath, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                imageFile.CopyTo(stream);

                if (!string.IsNullOrEmpty(existingBook.ImageUrl))
                {
                    var oldPath = Path.Combine(_environment.WebRootPath,
                        existingBook.ImageUrl.TrimStart('/'));

                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                existingBook.ImageUrl = "/uploads/books/" + fileName;
            }
            // =========================
            // PDF UPDATE
            // =========================
            if (pdfFile != null && pdfFile.Length > 0)
            {
                var pdfPath = Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "books",
                    "pdf"
                );

                Directory.CreateDirectory(pdfPath);

                var pdfName = Guid.NewGuid() + ".pdf";
                var fullPath = Path.Combine(pdfPath, pdfName);

                using var stream = new FileStream(fullPath, FileMode.Create);
                pdfFile.CopyTo(stream);

                // delete old pdf if exists
                if (!string.IsNullOrEmpty(existingBook.PdfUrl))
                {
                    var oldPdfPath = Path.Combine(
                        _environment.WebRootPath,
                        existingBook.PdfUrl.TrimStart('/')
                    );

                    if (System.IO.File.Exists(oldPdfPath))
                        System.IO.File.Delete(oldPdfPath);
                }

                existingBook.PdfUrl = "/uploads/books/pdf/" + pdfName;
            }

            // =========================
            // UPDATE BOOK
            // =========================
            existingBook.Title = model.Title;
            existingBook.Author = model.Author;
            existingBook.Publisher = model.Publisher;
            existingBook.Description = model.Description;
            existingBook.PublishDate = model.PublishDate;
            existingBook.Price = model.Price;
            existingBook.TotalCopies = model.TotalCopies;
            existingBook.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(model.ISBN))
                existingBook.ISBN = model.ISBN;

            // =========================
            // UPDATE CATEGORY MAPPINGS
            // =========================
            var oldMappings = _context.BookCategories
                .Where(bc => bc.BookId == model.BookId)
                .ToList();

            _context.BookCategories.RemoveRange(oldMappings);

            foreach (var categoryId in CategoryIds)
            {
                _context.BookCategories.Add(new BookCategory
                {
                    BookCategoryId = Guid.NewGuid(),
                    BookId = model.BookId,
                    CategoryId = categoryId
                });
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
        private void LoadCategories()
        {
            ViewBag.Categories = _context.Categories
                .OrderBy(c => c.OrderNo)
                .ToList();
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
