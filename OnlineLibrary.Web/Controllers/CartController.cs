using Microsoft.AspNetCore.Mvc;
using OnlineLibrary.Infrastructure.Data;
using OnlineLibrary.Infrastructure.Domain.Entities;
using OnlineLibrary.Web.Models;

namespace OnlineLibrary.Web.Controllers
{
    public class CartController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        public IActionResult Index()
        {
            if (!IsStudent())
                return RedirectToAction("Login", "Account");

            var userId = GetUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var cart = GetOrCreateCart(userId.Value);

            var cartItems =
                (from ci in _context.CartItems
                 join b in _context.Books on ci.BookId equals b.BookId
                 where ci.CartId == cart.CartId
                 select new CartItemViewModel
                 {
                     CartItemId = ci.CartItemId,
                     BookId = b.BookId,
                     Title = b.Title,
                     Author = b.Author,
                     ImageUrl = b.ImageUrl,
                     Price = b.Price,
                     Quantity = ci.Quantity,
                     AvailableCopies = b.TotalCopies
                 }).ToList();

            var model = new CartViewModel
            {
                Items = cartItems
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult Add(Guid bookId, int quantity = 1)
        {
            if (!IsStudent())
                return Json(new { success = false, message = "Please login to add items to cart." });

            var userId = GetUserId();
            if (userId == null)
                return Json(new { success = false, message = "Please login." });

            var book = _context.Books.Find(bookId);
            if (book == null)
                return Json(new { success = false, message = "Book not found." });

            if (book.TotalCopies < quantity)
                return Json(new { success = false, message = "Not enough copies available." });

            var cart = GetOrCreateCart(userId.Value);

            var existingItem = _context.CartItems
                .FirstOrDefault(ci => ci.CartId == cart.CartId && ci.BookId == bookId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;

                if (existingItem.Quantity > book.TotalCopies)
                    existingItem.Quantity = book.TotalCopies;
            }
            else
            {
                _context.CartItems.Add(new CartItem
                {
                    CartItemId = Guid.NewGuid(),
                    CartId = cart.CartId,
                    BookId = bookId,
                    Quantity = quantity,
                    AddedAt = DateTime.UtcNow
                });
            }

            cart.UpdatedAt = DateTime.UtcNow;
            _context.SaveChanges();

            var cartCount = _context.CartItems
                .Where(ci => ci.CartId == cart.CartId)
                .Sum(ci => ci.Quantity);

            return Json(new { success = true, message = "Added to cart!", cartCount });
        }

        [HttpPost]
        public IActionResult UpdateQuantity(Guid cartItemId, int quantity)
        {
            if (!IsStudent())
                return Json(new { success = false });

            var userId = GetUserId();
            if (userId == null)
                return Json(new { success = false });

            var cart = _context.Carts.FirstOrDefault(c => c.UserId == userId.Value);
            if (cart == null)
                return Json(new { success = false });

            var cartItem = _context.CartItems
                .FirstOrDefault(ci => ci.CartItemId == cartItemId && ci.CartId == cart.CartId);

            if (cartItem == null)
                return Json(new { success = false });

            var book = _context.Books.Find(cartItem.BookId);
            if (book == null)
                return Json(new { success = false });

            if (quantity <= 0)
            {
                _context.CartItems.Remove(cartItem);
            }
            else if (quantity <= book.TotalCopies)
            {
                cartItem.Quantity = quantity;
            }
            else
            {
                cartItem.Quantity = book.TotalCopies;
            }

            cart.UpdatedAt = DateTime.UtcNow;
            _context.SaveChanges();

            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult Remove(Guid cartItemId)
        {
            if (!IsStudent())
                return Json(new { success = false });

            var userId = GetUserId();
            if (userId == null)
                return Json(new { success = false });

            var cart = _context.Carts.FirstOrDefault(c => c.UserId == userId.Value);
            if (cart == null)
                return Json(new { success = false });

            var cartItem = _context.CartItems
                .FirstOrDefault(ci => ci.CartItemId == cartItemId && ci.CartId == cart.CartId);

            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                cart.UpdatedAt = DateTime.UtcNow;
                _context.SaveChanges();
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult Clear()
        {
            if (!IsStudent())
                return RedirectToAction("Login", "Account");

            var userId = GetUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var cart = _context.Carts.FirstOrDefault(c => c.UserId == userId.Value);
            if (cart != null)
            {
                var items = _context.CartItems.Where(ci => ci.CartId == cart.CartId);
                _context.CartItems.RemoveRange(items);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult GetCount()
        {
            var userId = GetUserId();
            if (userId == null)
                return Json(new { count = 0 });

            var cart = _context.Carts.FirstOrDefault(c => c.UserId == userId.Value);
            if (cart == null)
                return Json(new { count = 0 });

            var count = _context.CartItems
                .Where(ci => ci.CartId == cart.CartId)
                .Sum(ci => ci.Quantity);

            return Json(new { count });
        }

        private Cart GetOrCreateCart(Guid userId)
        {
            var cart = _context.Carts.FirstOrDefault(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart
                {
                    CartId = Guid.NewGuid(),
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Carts.Add(cart);
                _context.SaveChanges();
            }

            return cart;
        }

        private Guid? GetUserId()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
                return null;
            return Guid.Parse(userIdStr);
        }

        private bool IsStudent()
        {
            var roleId = HttpContext.Session.GetString("RoleId");
            if (string.IsNullOrEmpty(roleId))
                return false;

            var roleName = _context.Roles
                .Where(r => r.RoleId == Guid.Parse(roleId))
                .Select(r => r.RoleName)
                .FirstOrDefault();

            return roleName == "Student";
        }
    }
}