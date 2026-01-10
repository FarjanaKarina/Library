using Microsoft.EntityFrameworkCore;
using OnlineLibrary.Infrastructure.Domain.Entities;

namespace OnlineLibrary.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Core
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }

        // Library
        public DbSet<Category> Categories { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<BookCategory> BookCategories { get; set; }

        // Cart & Orders
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        // Payments
        public DbSet<Payment> Payments { get; set; }

        // Optional
        public DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<ContactMessage> ContactMessages { get; set; }
    }
}
