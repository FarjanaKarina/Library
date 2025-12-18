using Microsoft.EntityFrameworkCore;
using OnlineLibrary.Infrastructure.Domain.Entities;
using System.Collections.Generic;

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

        // Membership & Transactions
        public DbSet<Membership> Memberships { get; set; }
        public DbSet<BorrowTransaction> BorrowTransactions { get; set; }
        public DbSet<Fine> Fines { get; set; }

        // Optional
        public DbSet<Notification> Notifications { get; set; }
    }
}
