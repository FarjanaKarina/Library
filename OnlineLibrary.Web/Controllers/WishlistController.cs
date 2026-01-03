using Microsoft.AspNetCore.Mvc;
using OnlineLibrary.Infrastructure.Data;
using OnlineLibrary.Infrastructure.Domain.Entities;

public class WishlistController : Controller
{
    private readonly ApplicationDbContext _context;

    public WishlistController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public IActionResult Toggle(Guid bookId)
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr))
            return Unauthorized();

        var userId = Guid.Parse(userIdStr);

        var existing = _context.Wishlists
            .FirstOrDefault(w => w.UserId == userId && w.BookId == bookId);

        if (existing != null)
        {
            _context.Wishlists.Remove(existing);
            _context.SaveChanges();
            return Json(new { added = false });
        }

        _context.Wishlists.Add(new Wishlist
        {
            WishlistId = Guid.NewGuid(),
            UserId = userId,
            BookId = bookId
        });

        _context.SaveChanges();
        return Json(new { added = true });
    }

    public IActionResult Index()
    {
        var userId = Guid.Parse(HttpContext.Session.GetString("UserId"));

        var books = from w in _context.Wishlists
                    join b in _context.Books on w.BookId equals b.BookId
                    select b;

        return View(books.ToList());
    }

}
