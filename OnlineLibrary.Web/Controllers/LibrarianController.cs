using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineLibrary.Infrastructure.Data;
using OnlineLibrary.Infrastructure.Domain.Entities;
using OnlineLibrary.Infrastructure.Helpers;
using OnlineLibrary.Web.Models;
using System.Globalization;

namespace OnlineLibrary.Web.Controllers
{
    public class LibrarianController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        // =========================
        // LIBRARIAN DASHBOARD
        // =========================
        public IActionResult Dashboard()
        {
            if (!IsAuthorized())
                return RedirectToAction("Login", "Account");

            // =========================
            // SUMMARY STATS
            // =========================
            var totalBooks = _context.Books.Count();

            var totalOrders = _context.Orders
                .Count(o => o.PaymentStatus == "Success");

            var pendingOrders = _context.Orders
                .Count(o => o.PaymentStatus == "Success"
                    && (o.OrderStatus == "Confirmed" || o.OrderStatus == "Packed"));

            var pendingReturns = _context.OrderItems
                .Count(oi => oi.Status == "ReturnRequested");

            // =========================
            // RECENT ORDERS
            // =========================
            var recentOrders =
                (from o in _context.Orders
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
                 })
                .Take(10)
                .ToList();

            // =========================
            // PENDING RETURN REQUESTS
            // =========================
            var pendingReturnRequests =
                (from oi in _context.OrderItems
                 join o in _context.Orders on oi.OrderId equals o.OrderId
                 join u in _context.Users on o.UserId equals u.UserId
                 where oi.Status == "ReturnRequested"
                 orderby oi.ReturnRequestedAt
                 select new ReturnRequestViewModel
                 {
                     OrderItemId = oi.OrderItemId,
                     OrderId = o.OrderId,
                     TransactionId = o.TransactionId ?? "",
                     StudentName = u.FullName,
                     BookTitle = oi.BookTitle,
                     Price = oi.Price,
                     Quantity = oi.Quantity,
                     Status = oi.Status,
                     ReturnRequestedAt = oi.ReturnRequestedAt
                 })
                .Take(10)
                .ToList();

            var model = new LibrarianDashboardViewModel
            {
                TotalBooks = totalBooks,
                TotalOrders = totalOrders,
                PendingOrders = pendingOrders,
                PendingReturns = pendingReturns,
                RecentOrders = recentOrders,
                PendingReturnRequests = pendingReturnRequests
            };

            return View(model);
        }

        // =========================
        // ALL ORDERS
        // =========================
        public IActionResult Orders(string? status)
        {
            if (!IsAuthorized())
                return RedirectToAction("Login", "Account");

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
            return View(ordersQuery.ToList());
        }

        // =========================
        // ORDER DETAILS
        // =========================
        public IActionResult OrderDetails(Guid id)
        {
            if (!IsAuthorized())
                return RedirectToAction("Login", "Account");

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

            if (order == null)
                return NotFound();

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

            return View();
        }

        // =========================
        // UPDATE ORDER STATUS
        // =========================
        [HttpPost]
        public IActionResult UpdateOrderStatus(Guid orderId, string newStatus)
        {
            if (!IsAuthorized())
                return Json(new { success = false });

            var order = _context.Orders.Find(orderId);
            if (order == null)
                return Json(new { success = false, message = "Order not found" });

            var oldStatus = order.OrderStatus;
            order.OrderStatus = newStatus;

            if (newStatus == "Shipped")
                order.ShippedDate = DateTime.UtcNow;
            else if (newStatus == "Delivered")
                order.DeliveredDate = DateTime.UtcNow;

            _context.SaveChanges();

            // =========================
            // AUDIT LOG
            // =========================
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (userIdStr != null)
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    AuditLogId = Guid.NewGuid(),
                    ActorUserId = Guid.Parse(userIdStr),
                    ActorRole = "Librarian",
                    Action = "Order Status Updated",
                    EntityName = "Order",
                    EntityId = orderId,
                    Description = $"Order #{order.TransactionId} status changed from {oldStatus} to {newStatus}"
                });
                _context.SaveChanges();
            }

            // =========================
            // NOTIFY STUDENT
            // =========================
            var statusMessage = newStatus switch
            {
                "Packed" => "Your order has been packed and is ready for shipping.",
                "Shipped" => "Your order has been shipped! It will arrive soon.",
                "Delivered" => "Your order has been delivered. Enjoy your books!",
                _ => $"Your order status has been updated to: {newStatus}"
            };

            NotificationHelper.Send(
                _context,
                order.UserId,
                $"Order {newStatus}",
                $"Order #{order.TransactionId}: {statusMessage}",
                newStatus == "Delivered" ? "success" : "info");

            return Json(new { success = true });
        }

        // =========================
        // RETURN REQUESTS
        // =========================
        public IActionResult ReturnRequests()
        {
            if (!IsAuthorized())
                return RedirectToAction("Login", "Account");

            var requests =
                (from oi in _context.OrderItems
                 join o in _context.Orders on oi.OrderId equals o.OrderId
                 join u in _context.Users on o.UserId equals u.UserId
                 where oi.Status == "ReturnRequested" 
                    || oi.Status == "ReturnApproved" 
                    || oi.Status == "Received"
                    || oi.Status == "Refunded"
                 orderby oi.Status == "Refunded" ? 1 : 0, // Non-refunded first
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

            return View(requests);
        }

        // =========================
        // APPROVE RETURN
        // =========================
        [HttpPost]
        public IActionResult ApproveReturn(Guid orderItemId)
        {
            if (!IsAuthorized())
                return Json(new { success = false });

            var orderItem = _context.OrderItems.Find(orderItemId);
            if (orderItem == null || orderItem.Status != "ReturnRequested")
                return Json(new { success = false, message = "Invalid request" });

            orderItem.Status = "ReturnApproved";
            orderItem.ReturnApprovedAt = DateTime.UtcNow;
            _context.SaveChanges();

            // Get order for notification
            var order = _context.Orders.Find(orderItem.OrderId);

            // =========================
            // NOTIFY STUDENT
            // =========================
            if (order != null)
            {
                NotificationHelper.Send(
                    _context,
                    order.UserId,
                    "Return Approved",
                    $"Your return request for '{orderItem.BookTitle}' has been approved. Please ship the book back.",
                    "success");
            }

            return Json(new { success = true });
        }

        // =========================
        // MARK AS RECEIVED
        // =========================
        [HttpPost]
        public IActionResult MarkAsReceived(Guid orderItemId)
        {
            if (!IsAuthorized())
                return Json(new { success = false });

            var orderItem = _context.OrderItems.Find(orderItemId);
            if (orderItem == null || orderItem.Status != "ReturnApproved")
                return Json(new { success = false, message = "Invalid request" });

            orderItem.Status = "Received";
            orderItem.ReceivedAt = DateTime.UtcNow;

            // =========================
            // INCREASE INVENTORY
            // =========================
            var book = _context.Books.Find(orderItem.BookId);
            if (book != null)
            {
                book.TotalCopies += orderItem.Quantity;
            }

            _context.SaveChanges();

            // Get order for notification
            var order = _context.Orders.Find(orderItem.OrderId);

            // =========================
            // NOTIFY STUDENT
            // =========================
            if (order != null)
            {
                NotificationHelper.Send(
                    _context,
                    order.UserId,
                    "Book Received",
                    $"We have received '{orderItem.BookTitle}'. Your refund will be processed shortly.",
                    "info");
            }

            return Json(new { success = true });
        }

        // =========================
        // PROCESS REFUND
        // =========================
        [HttpPost]
        public IActionResult ProcessRefund(Guid orderItemId)
        {
            if (!IsAuthorized())
                return Json(new { success = false });

            var orderItem = _context.OrderItems.Find(orderItemId);
            if (orderItem == null || orderItem.Status != "Received")
                return Json(new { success = false, message = "Invalid request" });

            // Calculate 50% refund
            var refundAmount = orderItem.Price * orderItem.Quantity * 0.5m;

            orderItem.Status = "Refunded";
            orderItem.RefundedAt = DateTime.UtcNow;
            orderItem.RefundAmount = refundAmount;

            _context.SaveChanges();

            // Get order for notification
            var order = _context.Orders.Find(orderItem.OrderId);

            // =========================
            // AUDIT LOG
            // =========================
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (userIdStr != null)
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    AuditLogId = Guid.NewGuid(),
                    ActorUserId = Guid.Parse(userIdStr),
                    ActorRole = "Librarian",
                    Action = "Refund Processed",
                    EntityName = "OrderItem",
                    EntityId = orderItemId,
                    Description = $"Refund of ৳{refundAmount:N0} processed for '{orderItem.BookTitle}'"
                });
                _context.SaveChanges();
            }

            // =========================
            // NOTIFY STUDENT
            // =========================
            if (order != null)
            {
                NotificationHelper.Send(
                    _context,
                    order.UserId,
                    "Refund Processed",
                    $"Your refund of ৳{refundAmount:N0} for '{orderItem.BookTitle}' has been processed.",
                    "success");
            }

            return Json(new { success = true, refundAmount });
        }

        // =========================
        // REPORTS & ANALYTICS
        // =========================
        public IActionResult Reports()
        {
            if (!IsAuthorized())
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
                
            // 2. Category Distribution (FIXED QUERY)
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
        // REFUND HISTORY (LIBRARIAN)
        // =========================
        public IActionResult Refunds(DateTime? fromDate, DateTime? toDate)
        {
            if (!IsAuthorized())
                return RedirectToAction("Login", "Account");

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

            // Date filters
            if (fromDate.HasValue)
            {
                refundsQuery = refundsQuery.Where(r => r.RefundedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                refundsQuery = refundsQuery.Where(r => r.RefundedAt <= toDate.Value.AddDays(1));
            }

            var refunds = refundsQuery.ToList();

            // Summary stats
            var allRefunds = _context.OrderItems.Where(oi => oi.Status == "Refunded");
            var thisMonth = DateTime.UtcNow.Month;
            var thisYear = DateTime.UtcNow.Year;

            var model = new RefundSummaryViewModel
            {
                TotalRefunds = allRefunds.Count(),
                TotalRefundAmount = allRefunds.Sum(oi => oi.RefundAmount ?? 0),
                RefundsThisMonth = allRefunds.Count(oi => oi.RefundedAt.HasValue && 
                    oi.RefundedAt.Value.Month == thisMonth && 
                    oi.RefundedAt.Value.Year == thisYear),
                RefundAmountThisMonth = allRefunds
                    .Where(oi => oi.RefundedAt.HasValue && 
                        oi.RefundedAt.Value.Month == thisMonth && 
                        oi.RefundedAt.Value.Year == thisYear)
                    .Sum(oi => oi.RefundAmount ?? 0),
                Refunds = refunds
            };

            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;

            return View(model);
        }

        // =========================
        // ROLE CHECK (UPDATED)
        // =========================
        private bool IsAuthorized()
        {
            var roleId = HttpContext.Session.GetString("RoleId");
            if (string.IsNullOrEmpty(roleId))
                return false;

            var roleName = _context.Roles
                .Where(r => r.RoleId == Guid.Parse(roleId))
                .Select(r => r.RoleName)
                .FirstOrDefault();

            return roleName == "Librarian" || roleName == "Admin"; // ✅ Already allows Admin!
        }

        private bool IsLibrarian() // Keep for specific librarian-only actions if any
        {
            var roleId = HttpContext.Session.GetString("RoleId");
            if (string.IsNullOrEmpty(roleId))
                return false;

            var roleName = _context.Roles
                .Where(r => r.RoleId == Guid.Parse(roleId))
                .Select(r => r.RoleName)
                .FirstOrDefault();

            return roleName == "Librarian";
        }

        // =========================
        // READING ANALYTICS
        // =========================
        public IActionResult ReadingAnalytics()
        {
            if (!IsAuthorized())
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
    }
}