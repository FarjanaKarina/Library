using Microsoft.AspNetCore.Mvc;
using OnlineLibrary.Infrastructure.Data;
using OnlineLibrary.Infrastructure.Domain.Entities;
using OnlineLibrary.Infrastructure.Helpers;
using OnlineLibrary.Infrastructure.Security;
using OnlineLibrary.Web.Models;
using System.Globalization;

namespace OnlineLibrary.Web.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // Admin Dashboard
        // =========================
        public IActionResult Dashboard()
        {
            // 1️⃣ Check login
            var userId = HttpContext.Session.GetString("UserId");
            var roleId = HttpContext.Session.GetString("RoleId");

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(roleId))
            {
                return RedirectToAction("Login", "Account");
            }

            // 2️⃣ Check role = Admin
            var roleName = _context.Roles
                .Where(r => r.RoleId == Guid.Parse(roleId))
                .Select(r => r.RoleName)
                .FirstOrDefault();

            if (roleName != "Admin")
            {
                return RedirectToAction("Index", "Home");
            }

            // 3️⃣ Dashboard statistics
            var studentRoleId = _context.Roles
                .Where(r => r.RoleName == "Student")
                .Select(r => r.RoleId)
                .FirstOrDefault();

            ViewBag.TotalUsers = _context.Users.Count(u => u.RoleId == studentRoleId);
            ViewBag.TotalBooks = _context.Books.Count();
            ViewBag.TotalCategories = _context.Categories.Count();

            var librarianRoleId = _context.Roles
                .Where(r => r.RoleName == "Librarian")
                .Select(r => r.RoleId)
                .FirstOrDefault();

            ViewBag.ActiveLibrarians = _context.Users
                .Count(u => u.RoleId == librarianRoleId && u.IsActive);

            // 4️⃣ Recent Messages
            ViewBag.RecentMessages = _context.ContactMessages
                .OrderByDescending(m => m.CreatedAt)
                .Take(5)
                .ToList();

            // 5️⃣ Pending Memberships (Assuming they are in the User table with a specific flag or separate table)
            // Let's check the database for standard membership context
            ViewBag.PendingMemberships = _context.Notifications.Count(n => n.Title.Contains("Membership Request")); // Fallback/Placeholder if specific table isn't clear

            return View();
        }

        [HttpPost]
        public IActionResult SendAnnouncement(string title, string message)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            NotificationHelper.Broadcast(
                _context,
                title,
                message,
                "system"
            );

            return RedirectToAction("Dashboard");
        }


       
        // =========================
        // CREATE LIBRARIAN (GET)
        // =========================
        public IActionResult CreateLibrarian()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            return View();
        }

        // =========================
        // CREATE LIBRARIAN (POST)
        // =========================
        [HttpPost]
        public IActionResult CreateLibrarian(LibrarianCreateViewModel model)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            // Email uniqueness
            var emailExists = _context.Users.Any(u => u.Email == model.Email);
            if (emailExists)
            {
                ViewBag.Error = "Email already exists.";
                return View(model);
            }

            // Get Librarian role
            var librarianRole = _context.Roles.First(r => r.RoleName == "Librarian");

            var librarian = new User
            {
                UserId = Guid.NewGuid(),
                FullName = model.FullName,
                Email = model.Email,
                PasswordHash = PasswordHelper.HashPassword(model.Password),
                Phone = model.Phone,
                Address = model.Address,
                RoleId = librarianRole.RoleId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(librarian);
            _context.SaveChanges();

            return RedirectToAction("Librarians");
        }

        [HttpPost]
        public IActionResult DeleteLibrarian(Guid id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var librarian = _context.Users.Find(id);
            if (librarian == null) return NotFound();

            // Optional: Check if librarian has active orders or other dependencies before deleting
            // For simplicity, we'll just remove them here.
            
            _context.Users.Remove(librarian);
            _context.SaveChanges();

            return RedirectToAction("Librarians");
        }

        // =========================
        // User Management
        // =========================
        public IActionResult Users()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var studentRoleId = _context.Roles
                .Where(r => r.RoleName == "Student")
                .Select(r => r.RoleId)
                .FirstOrDefault();

            var users = _context.Users
                .Where(u => u.RoleId == studentRoleId)
                .OrderByDescending(u => u.CreatedAt)
                .ToList();

            return View(users);
        }

        [HttpPost]
        public IActionResult ToggleUserStatus(Guid id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var user = _context.Users.Find(id);
            if (user == null) return NotFound();

            user.IsActive = !user.IsActive;
            _context.SaveChanges();

            return RedirectToAction("Users");
        }

        [HttpPost]
        public IActionResult DeleteUser(Guid id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var user = _context.Users.Find(id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            _context.SaveChanges();

            return RedirectToAction("Users");
        }

        // =========================
        // List Librarians
        // =========================
        public IActionResult Librarians()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var librarianRoleId = _context.Roles
                .Where(r => r.RoleName == "Librarian")
                .Select(r => r.RoleId)
                .First();

            var librarians = _context.Users
                .Where(u => u.RoleId == librarianRoleId)
                .ToList();

            return View(librarians);
        }
        // =========================
        // REPORTS & ANALYTICS
        // =========================
        public IActionResult Reports()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            // 1. Monthly Sales Data (Last 12 Months)
            var monthlySales = _context.Orders
                .Where(o => o.PaymentStatus == "Success" && o.OrderDate >= DateTime.UtcNow.AddYears(-1))
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Total = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(s => s.Year).ThenBy(s => s.Month)
                .ToList();

            var salesData = new ChartData();
            var culture = CultureInfo.CreateSpecificCulture("en-US");
            for (int i = 11; i >= 0; i--)
            {
                var date = DateTime.UtcNow.AddMonths(-i);
                var monthData = monthlySales.FirstOrDefault(s => s.Year == date.Year && s.Month == date.Month);
                salesData.Labels.Add(date.ToString("MMM yyyy", culture));
                salesData.Data.Add(monthData?.Total ?? 0);
            }
                
            // 2. Category Distribution
            var categoryDistribution = (from oi in _context.OrderItems
                                        join b in _context.Books on oi.BookId equals b.BookId
                                        join c in _context.Categories on b.CategoryId equals c.CategoryId
                                        group oi by c.CategoryName into g
                                        select new
                                        {
                                            Category = g.Key,
                                            Count = g.Sum(oi => oi.Quantity)
                                        })
                                        .OrderByDescending(c => c.Count)
                                        .ToList();

            var categoryData = new ChartData
            {
                Labels = categoryDistribution.Select(c => c.Category).ToList(),
                Data = categoryDistribution.Select(c => (decimal)c.Count).ToList()
            };

            // 3. Top Selling Books
            var topSellingBooks = _context.OrderItems
                .GroupBy(oi => new { oi.BookId, oi.BookTitle })
                .Select(g => new
                {
                    BookId = g.Key.BookId,
                    Title = g.Key.BookTitle,
                    Count = g.Sum(oi => oi.Quantity)
                })
                .OrderByDescending(b => b.Count)
                .Take(5)
                .Join(_context.Books,
                      orderInfo => orderInfo.BookId,
                      book => book.BookId,
                      (orderInfo, book) => new BookPerformanceViewModel
                      {
                          Title = orderInfo.Title,
                          OrderCount = orderInfo.Count,
                          ImageUrl = book.ImageUrl
                      })
                .ToList();

            // 4. Key Metrics
            var metrics = new ReportMetricsViewModel
            {
                TotalRevenue = _context.Orders.Where(o => o.PaymentStatus == "Success").Sum(o => o.TotalAmount),
                TotalOrders = _context.Orders.Count(o => o.PaymentStatus == "Success"),
                TotalRefunds = _context.OrderItems.Where(oi => oi.Status == "Refunded").Sum(oi => oi.RefundAmount ?? 0),
                ReturnedItems = _context.OrderItems.Count(oi => oi.Status == "Refunded" || oi.Status == "Received")
            };

            var model = new ReportViewModel
            {
                MonthlySales = salesData,
                CategoryDistribution = categoryData,
                TopSellingBooks = topSellingBooks,
                Metrics = metrics
            };

            return View(model);
        }

        // =========================
        // READING ANALYTICS
        // =========================
        public IActionResult ReadingAnalytics()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var today = DateTime.UtcNow.Date;
            var weekAgo = DateTime.UtcNow.AddDays(-7);

            // Get all reading activity (books with LastReadAt set)
            var readingData = (from w in _context.Wishlists
                               join b in _context.Books on w.BookId equals b.BookId
                               join u in _context.Users on w.UserId equals u.UserId
                               where w.LastReadAt != null
                               select new
                               {
                                   w.BookId,
                                   b.Title,
                                   b.Author,
                                   b.ImageUrl,
                                   w.UserId,
                                   u.FullName,
                                   u.Email,
                                   w.LastReadAt
                               }).ToList();

            // Group by book
            var bookStats = readingData
                .GroupBy(r => new { r.BookId, r.Title, r.Author, r.ImageUrl })
                .Select(g => new BookReadingStats
                {
                    BookId = g.Key.BookId,
                    Title = g.Key.Title,
                    Author = g.Key.Author,
                    ImageUrl = g.Key.ImageUrl,
                    ReaderCount = g.Count(),
                    LastReadTime = g.Max(x => x.LastReadAt),
                    Readers = g.Select(x => new ReaderInfo
                    {
                        UserId = x.UserId,
                        FullName = x.FullName,
                        Email = x.Email,
                        LastReadAt = x.LastReadAt
                    })
                    .OrderByDescending(x => x.LastReadAt)
                    .ToList()
                })
                .OrderByDescending(b => b.ReaderCount)
                .ThenByDescending(b => b.LastReadTime)
                .ToList();

            var model = new ReadingAnalyticsViewModel
            {
                TotalActiveReaders = readingData.Select(r => r.UserId).Distinct().Count(),
                TotalBooksBeingRead = bookStats.Count,
                ReadersToday = readingData.Count(r => r.LastReadAt >= today),
                ReadersThisWeek = readingData.Count(r => r.LastReadAt >= weekAgo),
                BookStats = bookStats
            };

            return View(model);
        }

        // =========================
        // AUDIT LOGS (READ ONLY)
        // =========================
        public IActionResult AuditLogs()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var logs =
                from a in _context.AuditLogs
                join u in _context.Users
                    on a.ActorUserId equals u.UserId
                orderby a.CreatedAt descending
                select new AdminAuditLogViewModel
                {
                    ActorName = u.FullName,
                    ActorRole = a.ActorRole,
                    Action = a.Action,
                    EntityName = a.EntityName,
                    Description = a.Description,
                    CreatedAt = a.CreatedAt
                };

            return View(logs.ToList());
        }

        // =========================
        // CONTACT MESSAGES
        // =========================
        public IActionResult Messages()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var messages = _context.ContactMessages
                .OrderByDescending(m => m.CreatedAt)
                .ToList();

            return View(messages);
        }

        [HttpPost]
        public async Task<IActionResult> MarkMessageRead(Guid id)
        {
            if (!IsAdmin()) return Json(new { success = false });

            var message = await _context.ContactMessages.FindAsync(id);
            if (message == null) return NotFound();

            message.IsRead = true;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }


        // =========================
        // SHARED OPERATIONAL TASKS (ADMIN VIEW)
        // =========================

        public IActionResult Orders(string? status)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var ordersQuery =
                from o in _context.Orders
                join u in _context.Users on o.UserId equals u.UserId
                where o.PaymentStatus == "Success"
                orderby o.OrderDate descending
                select new RecentOrderViewModel
                {
                    OrderId = o.OrderId,
                    TransactionId = o.TransactionId ?? "",
                    StudentName = u.FullName,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    OrderStatus = o.OrderStatus,
                    ItemCount = _context.OrderItems.Count(oi => oi.OrderId == o.OrderId)
                };

            if (!string.IsNullOrEmpty(status))
            {
                ordersQuery = ordersQuery.Where(o => o.OrderStatus == status);
            }

            ViewBag.CurrentStatus = status;
            return View("~/Views/Librarian/Orders.cshtml", ordersQuery.ToList());
        }

        public IActionResult OrderDetails(Guid id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var order = (from o in _context.Orders
                         join u in _context.Users on o.UserId equals u.UserId
                         where o.OrderId == id
                         select new
                         {
                             Order = o,
                             StudentName = u.FullName,
                             StudentEmail = u.Email,
                             StudentPhone = u.Phone
                         }).FirstOrDefault();

            if (order == null) return NotFound();

            var orderItems = (from oi in _context.OrderItems
                              join b in _context.Books on oi.BookId equals b.BookId
                              where oi.OrderId == id
                              select new
                              {
                                  oi.OrderItemId,
                                  oi.BookId,
                                  oi.BookTitle,
                                  oi.Price,
                                  oi.Quantity,
                                  oi.Status,
                                  oi.ReturnRequestedAt,
                                  oi.ReturnApprovedAt,
                                  oi.ReceivedAt,
                                  oi.RefundedAt,
                                  oi.RefundAmount,
                                  b.ImageUrl
                              }).ToList();

            ViewBag.Order = order.Order;
            ViewBag.StudentName = order.StudentName;
            ViewBag.StudentEmail = order.StudentEmail;
            ViewBag.StudentPhone = order.StudentPhone;
            ViewBag.OrderItems = orderItems;

            return View("~/Views/Librarian/OrderDetails.cshtml");
        }

        public IActionResult ReturnRequests()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var requests =
                (from oi in _context.OrderItems
                 join o in _context.Orders on oi.OrderId equals o.OrderId
                 join u in _context.Users on o.UserId equals u.UserId
                 where oi.Status == "ReturnRequested" 
                    || oi.Status == "ReturnApproved" 
                    || oi.Status == "Received"
                    || oi.Status == "Refunded"
                 orderby oi.Status == "Refunded" ? 1 : 0,
                         oi.ReturnRequestedAt descending
                 select new ReturnRequestViewModel
                 {
                     OrderItemId = oi.OrderItemId,
                     OrderId = o.OrderId,
                     TransactionId = o.TransactionId ?? "",
                     StudentName = u.FullName,
                     StudentEmail = u.Email,
                     BookTitle = oi.BookTitle,
                     Price = oi.Price,
                     Quantity = oi.Quantity,
                     Status = oi.Status,
                     ReturnRequestedAt = oi.ReturnRequestedAt,
                     ReturnApprovedAt = oi.ReturnApprovedAt,
                     ReceivedAt = oi.ReceivedAt,
                     RefundedAt = oi.RefundedAt,
                     ActualRefundAmount = oi.RefundAmount
                 }).ToList();

            return View("~/Views/Librarian/ReturnRequests.cshtml", requests);
        }

        public IActionResult Refunds(DateTime? fromDate, DateTime? toDate)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var refundsQuery = from oi in _context.OrderItems
                               join o in _context.Orders on oi.OrderId equals o.OrderId
                               join u in _context.Users on o.UserId equals u.UserId
                               join b in _context.Books on oi.BookId equals b.BookId
                               where oi.Status == "Refunded"
                               orderby oi.RefundedAt descending
                               select new RefundViewModel
                               {
                                   OrderItemId = oi.OrderItemId,
                                   OrderId = o.OrderId,
                                   TransactionId = o.TransactionId ?? "",
                                   StudentName = u.FullName,
                                   StudentEmail = u.Email,
                                   BookTitle = oi.BookTitle,
                                   OriginalPrice = oi.Price,
                                   Quantity = oi.Quantity,
                                   RefundAmount = oi.RefundAmount ?? 0,
                                   RefundedAt = oi.RefundedAt,
                                   BookImageUrl = b.ImageUrl
                               };

            if (fromDate.HasValue)
                refundsQuery = refundsQuery.Where(r => r.RefundedAt >= fromDate.Value);
            if (toDate.HasValue)
                refundsQuery = refundsQuery.Where(r => r.RefundedAt <= toDate.Value);

            var refunds = refundsQuery.ToList();

            var model = new RefundSummaryViewModel
            {
                Refunds = refunds,
                TotalRefunds = refunds.Count,
                TotalRefundAmount = refunds.Sum(r => r.RefundAmount),
                RefundsThisMonth = refunds.Count(r => r.RefundedAt >= new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1)),
                RefundAmountThisMonth = refunds.Where(r => r.RefundedAt >= new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1)).Sum(r => r.RefundAmount)
            };

            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;

            return View("~/Views/Librarian/Refunds.cshtml", model);
        }

        [HttpPost]
        public IActionResult UpdateOrderStatus(Guid orderId, string newStatus)
        {
            if (!IsAdmin()) return Json(new { success = false });

            var order = _context.Orders.Find(orderId);
            if (order == null) return Json(new { success = false, message = "Order not found" });

            var oldStatus = order.OrderStatus;
            order.OrderStatus = newStatus;

            if (newStatus == "Shipped") order.ShippedDate = DateTime.UtcNow;
            else if (newStatus == "Delivered") order.DeliveredDate = DateTime.UtcNow;

            _context.SaveChanges();

            // Audit
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (userIdStr != null)
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    AuditLogId = Guid.NewGuid(),
                    ActorUserId = Guid.Parse(userIdStr),
                    ActorRole = "Admin",
                    Action = "Order Status Updated",
                    EntityName = "Order",
                    EntityId = orderId,
                    Description = $"Order #{order.TransactionId} status changed from {oldStatus} to {newStatus}"
                });
                _context.SaveChanges();
            }

            // Notify
            NotificationHelper.Send(_context, order.UserId, $"Order {newStatus}", $"Order #{order.TransactionId}: Status updated to {newStatus}", "info");

            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult ApproveReturn(Guid orderItemId)
        {
            if (!IsAdmin()) return Json(new { success = false });

            var orderItem = _context.OrderItems.Find(orderItemId);
            if (orderItem == null || orderItem.Status != "ReturnRequested")
                return Json(new { success = false, message = "Invalid request" });

            orderItem.Status = "ReturnApproved";
            orderItem.ReturnApprovedAt = DateTime.UtcNow;
            _context.SaveChanges();

            var order = _context.Orders.Find(orderItem.OrderId);
            if (order != null)
                NotificationHelper.Send(_context, order.UserId, "Return Approved", $"Return for '{orderItem.BookTitle}' approved.", "success");

            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult MarkAsReceived(Guid orderItemId)
        {
            if (!IsAdmin()) return Json(new { success = false });

            var orderItem = _context.OrderItems.Find(orderItemId);
            if (orderItem == null || orderItem.Status != "ReturnApproved")
                return Json(new { success = false, message = "Invalid request" });

            orderItem.Status = "Received";
            orderItem.ReceivedAt = DateTime.UtcNow;

            var book = _context.Books.Find(orderItem.BookId);
            if (book != null) book.TotalCopies += orderItem.Quantity;

            _context.SaveChanges();

            var order = _context.Orders.Find(orderItem.OrderId);
            if (order != null)
                NotificationHelper.Send(_context, order.UserId, "Book Received", $"We received '{orderItem.BookTitle}'.", "info");

            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult ProcessRefund(Guid orderItemId)
        {
            if (!IsAdmin()) return Json(new { success = false });

            var orderItem = _context.OrderItems.Find(orderItemId);
            if (orderItem == null || orderItem.Status != "Received")
                return Json(new { success = false, message = "Invalid request" });

            var refundAmount = orderItem.Price * orderItem.Quantity * 0.5m;
            orderItem.Status = "Refunded";
            orderItem.RefundedAt = DateTime.UtcNow;
            orderItem.RefundAmount = refundAmount;
            _context.SaveChanges();

            // Audit
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (userIdStr != null)
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    AuditLogId = Guid.NewGuid(),
                    ActorUserId = Guid.Parse(userIdStr),
                    ActorRole = "Admin",
                    Action = "Refund Processed",
                    EntityName = "OrderItem",
                    EntityId = orderItemId,
                    Description = $"Refund of ৳{refundAmount:N0} processed for '{orderItem.BookTitle}'"
                });
                _context.SaveChanges();
            }

            var order = _context.Orders.Find(orderItem.OrderId);
            if (order != null)
                NotificationHelper.Send(_context, order.UserId, "Refund Processed", $"Refund of ৳{refundAmount:N0} for '{orderItem.BookTitle}' processed.", "success");

            return Json(new { success = true, refundAmount });
        }

        // =========================
        // Helper: Is Admin
        // =========================
        private bool IsAdmin()
        {
            var roleId = HttpContext.Session.GetString("RoleId");
            if (string.IsNullOrEmpty(roleId))
                return false;

            var roleName = _context.Roles
                .Where(r => r.RoleId == Guid.Parse(roleId))
                .Select(r => r.RoleName)
                .FirstOrDefault();

            ViewBag.CurrentRole = roleName;
    return roleName == "Admin";
        }
    }
}
