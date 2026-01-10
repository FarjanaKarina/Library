using Microsoft.AspNetCore.Mvc;
using OnlineLibrary.Infrastructure.Data;
using OnlineLibrary.Infrastructure.Domain.Entities;
using OnlineLibrary.Infrastructure.Helpers;
using OnlineLibrary.Web.Models;

namespace OnlineLibrary.Web.Controllers
{
    public class OrderController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        // =========================
        // MY ORDERS
        // =========================
        public IActionResult MyOrders()
        {
            if (!IsStudent())
                return RedirectToAction("Login", "Account");

            var userId = GetUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var orders = (from o in _context.Orders
                          where o.UserId == userId.Value
                          orderby o.OrderDate descending
                          select new OrderSummaryViewModel
                          {
                              OrderId = o.OrderId,
                              TransactionId = o.TransactionId ?? "",
                              OrderDate = o.OrderDate,
                              TotalAmount = o.TotalAmount,
                              OrderStatus = o.OrderStatus,
                              PaymentStatus = o.PaymentStatus,
                              TotalItems = _context.OrderItems.Count(oi => oi.OrderId == o.OrderId),
                              CanRequestReturn = o.OrderStatus == "Delivered"
                          }).ToList();

            var model = new MyOrdersViewModel
            {
                Orders = orders
            };

            return View(model);
        }

        // =========================
        // ORDER DETAILS
        // =========================
        public IActionResult Details(Guid id)
        {
            if (!IsStudent())
                return RedirectToAction("Login", "Account");

            var userId = GetUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var order = _context.Orders.FirstOrDefault(o => o.OrderId == id && o.UserId == userId.Value);
            if (order == null)
                return NotFound();

            var orderItems = (from oi in _context.OrderItems
                              join b in _context.Books on oi.BookId equals b.BookId
                              where oi.OrderId == id
                              select new OrderItemDetailViewModel
                              {
                                  OrderItemId = oi.OrderItemId,
                                  BookId = oi.BookId,
                                  BookTitle = oi.BookTitle,
                                  ImageUrl = b.ImageUrl,
                                  Price = oi.Price,
                                  Quantity = oi.Quantity,
                                  Status = oi.Status,
                                  ReturnRequestedAt = oi.ReturnRequestedAt,
                                  ReturnApprovedAt = oi.ReturnApprovedAt,
                                  ReceivedAt = oi.ReceivedAt,
                                  RefundedAt = oi.RefundedAt,
                                  RefundAmount = oi.RefundAmount
                              }).ToList();

            var model = new OrderDetailsViewModel
            {
                OrderId = order.OrderId,
                TransactionId = order.TransactionId ?? "",
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                OrderStatus = order.OrderStatus,
                PaymentStatus = order.PaymentStatus,
                ShippingName = order.ShippingName,
                ShippingPhone = order.ShippingPhone,
                ShippingAddress = order.ShippingAddress,
                BankTransactionId = order.BankTransactionId,
                CardType = order.CardType,
                PaymentDate = order.PaymentDate,
                Items = orderItems
            };

            return View(model);
        }

        // =========================
        // REQUEST RETURN
        // =========================
        [HttpPost]
        public IActionResult RequestReturn(Guid orderItemId, string accountNumber, string paymentMethod)
        {
            if (!IsStudent())
                return Json(new { success = false, message = "Unauthorized" });

            var userId = GetUserId();
            if (userId == null)
                return Json(new { success = false, message = "Please login" });

            var orderItem = _context.OrderItems.Find(orderItemId);
            if (orderItem == null)
                return Json(new { success = false, message = "Order item not found" });

            // Verify ownership
            var order = _context.Orders.Find(orderItem.OrderId);
            if (order == null || order.UserId != userId.Value)
                return Json(new { success = false, message = "Unauthorized" });

            // Check if already requested
            if (orderItem.Status != "Active")
                return Json(new { success = false, message = "Return already requested or processed" });

            // Update status and refund info
            orderItem.Status = "ReturnRequested";
            orderItem.ReturnRequestedAt = DateTime.UtcNow;
            orderItem.RefundAccountNumber = accountNumber;
            orderItem.RefundPaymentMethod = paymentMethod;
            _context.SaveChanges();

            // Notify librarians
            var librarianIds = _context.Users
                .Where(u => _context.Roles.Any(r => r.RoleId == u.RoleId && r.RoleName == "Librarian"))
                .Select(u => u.UserId)
                .ToList();

            foreach (var libId in librarianIds)
            {
                NotificationHelper.Send(
                    _context,
                    libId,
                    "Return Request",
                    $"Return request for '{orderItem.BookTitle}' from order #{order.TransactionId}",
                    "info");
            }

            // Notify student
            NotificationHelper.Send(
                _context,
                userId.Value,
                "Return Request Submitted",
                $"Your return request for '{orderItem.BookTitle}' has been submitted. Waiting for approval.",
                "info");

            return Json(new { success = true, message = "Return request submitted successfully" });
        }

        // =========================
        // CANCEL RETURN REQUEST
        // =========================
        [HttpPost]
        public IActionResult CancelReturn(Guid orderItemId)
        {
            if (!IsStudent())
                return Json(new { success = false, message = "Unauthorized" });

            var userId = GetUserId();
            if (userId == null)
                return Json(new { success = false, message = "Please login" });

            var orderItem = _context.OrderItems.Find(orderItemId);
            if (orderItem == null)
                return Json(new { success = false, message = "Order item not found" });

            var order = _context.Orders.Find(orderItem.OrderId);
            if (order == null || order.UserId != userId.Value)
                return Json(new { success = false, message = "Unauthorized" });

            if (orderItem.Status != "ReturnRequested")
                return Json(new { success = false, message = "Cannot cancel this return request" });

            orderItem.Status = "Active";
            orderItem.ReturnRequestedAt = null;
            _context.SaveChanges();

            NotificationHelper.Send(
                _context,
                userId.Value,
                "Return Request Cancelled",
                $"Your return request for '{orderItem.BookTitle}' has been cancelled.",
                "info");

            return Json(new { success = true, message = "Return request cancelled" });
        }

        // =========================
        // HELPERS
        // =========================
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