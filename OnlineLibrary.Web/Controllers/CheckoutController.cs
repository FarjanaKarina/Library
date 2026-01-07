using Microsoft.AspNetCore.Mvc;
using OnlineLibrary.Infrastructure.Data;
using OnlineLibrary.Infrastructure.Domain.Entities;
using OnlineLibrary.Infrastructure.Helpers;
using OnlineLibrary.Web.Models;

namespace OnlineLibrary.Web.Controllers
{
    public class CheckoutController(ApplicationDbContext context, IConfiguration configuration) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IConfiguration _configuration = configuration;
        private readonly HttpClient _httpClient = new();

        public IActionResult Index()
        {
            if (!IsStudent())
                return RedirectToAction("Login", "Account");

            var userId = GetUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var user = _context.Users.Find(userId.Value);
            var cart = _context.Carts.FirstOrDefault(c => c.UserId == userId.Value);

            if (cart == null)
                return RedirectToAction("Index", "Cart");

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

            if (cartItems.Count == 0)
                return RedirectToAction("Index", "Cart");

            var model = new CheckoutViewModel
            {
                CartItems = cartItems,
                ShippingName = user?.FullName ?? "",
                ShippingPhone = user?.Phone ?? "",
                ShippingAddress = user?.Address ?? ""
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(PlaceOrderViewModel model)
        {
            if (!IsStudent())
                return RedirectToAction("Login", "Account");

            var userId = GetUserId();
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var user = _context.Users.Find(userId.Value);
            var cart = _context.Carts.FirstOrDefault(c => c.UserId == userId.Value);

            if (cart == null)
                return RedirectToAction("Index", "Cart");

            var cartItems =
                (from ci in _context.CartItems
                 join b in _context.Books on ci.BookId equals b.BookId
                 where ci.CartId == cart.CartId
                 select new
                 {
                     ci.CartItemId,
                     ci.BookId,
                     b.Title,
                     b.Price,
                     ci.Quantity,
                     b.TotalCopies
                 }).ToList();

            if (cartItems.Count == 0)
                return RedirectToAction("Index", "Cart");

            foreach (var item in cartItems)
            {
                if (item.Quantity > item.TotalCopies)
                {
                    TempData["Error"] = $"Not enough copies of '{item.Title}' available.";
                    return RedirectToAction("Index", "Cart");
                }
            }

            decimal totalAmount = cartItems.Sum(i => i.Price * i.Quantity);

            var transactionId = $"ORD{DateTime.Now:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

            var order = new Order
            {
                OrderId = Guid.NewGuid(),
                UserId = userId.Value,
                TotalAmount = totalAmount,
                OrderStatus = "Pending",
                ShippingName = model.ShippingName,
                ShippingPhone = model.ShippingPhone,
                ShippingAddress = model.ShippingAddress,
                TransactionId = transactionId,
                PaymentStatus = "Pending",
                OrderDate = DateTime.UtcNow
            };

            _context.Orders.Add(order);

            foreach (var item in cartItems)
            {
                _context.OrderItems.Add(new OrderItem
                {
                    OrderItemId = Guid.NewGuid(),
                    OrderId = order.OrderId,
                    BookId = item.BookId,
                    BookTitle = item.Title,
                    Price = item.Price,
                    Quantity = item.Quantity,
                    Status = "Active"
                });
            }

            _context.SaveChanges();

            var storeId = _configuration["SSLCommerz:StoreId"] ?? "";
            var storePassword = _configuration["SSLCommerz:StorePassword"] ?? "";
            var sessionApiUrl = _configuration["SSLCommerz:SessionApiUrl"] ?? "";
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var productNames = string.Join(", ", cartItems.Select(i => i.Title).Take(3));
            if (cartItems.Count > 3)
                productNames += $" and {cartItems.Count - 3} more";

            var postData = new Dictionary<string, string>
            {
                ["store_id"] = storeId,
                ["store_passwd"] = storePassword,
                ["total_amount"] = totalAmount.ToString("F2"),
                ["currency"] = "BDT",
                ["tran_id"] = transactionId,
                ["success_url"] = $"{baseUrl}/Checkout/Success",
                ["fail_url"] = $"{baseUrl}/Checkout/Fail",
                ["cancel_url"] = $"{baseUrl}/Checkout/Cancel",
                ["ipn_url"] = $"{baseUrl}/Checkout/IPN",
                ["cus_name"] = model.ShippingName,
                ["cus_email"] = user?.Email ?? "customer@example.com",
                ["cus_phone"] = model.ShippingPhone,
                ["cus_add1"] = model.ShippingAddress,
                ["cus_city"] = "Dhaka",
                ["cus_country"] = "Bangladesh",
                ["shipping_method"] = "Courier",
                ["product_name"] = productNames,
                ["product_category"] = "Books",
                ["product_profile"] = "physical-goods",
                ["num_of_item"] = cartItems.Count.ToString()
            };

            var content = new FormUrlEncodedContent(postData);
            var response = await _httpClient.PostAsync(sessionApiUrl, content);
            var responseString = await response.Content.ReadAsStringAsync();

            try
            {
                var json = System.Text.Json.JsonDocument.Parse(responseString);
                var status = json.RootElement.GetProperty("status").GetString();

                if (status == "SUCCESS")
                {
                    var gatewayUrl = json.RootElement.GetProperty("GatewayPageURL").GetString();
                    var sessionKey = json.RootElement.GetProperty("sessionkey").GetString();

                    order.SessionKey = sessionKey;
                    _context.SaveChanges();

                    if (!string.IsNullOrEmpty(gatewayUrl))
                        return Redirect(gatewayUrl);
                }

                var failedReason = json.RootElement.TryGetProperty("failedreason", out var reason)
                    ? reason.GetString()
                    : "Unknown error";

                TempData["Error"] = $"Payment initiation failed: {failedReason}";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error processing payment: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public IActionResult Success()
        {
            var transactionId = Request.Form["tran_id"].ToString();
            var bankTransactionId = Request.Form["bank_tran_id"].ToString();
            var cardType = Request.Form["card_type"].ToString();
            var status = Request.Form["status"].ToString();

            var order = _context.Orders.FirstOrDefault(o => o.TransactionId == transactionId);

            if (order != null && status == "VALID")
            {
                order.PaymentStatus = "Success";
                order.OrderStatus = "Confirmed";
                order.BankTransactionId = bankTransactionId;
                order.CardType = cardType;
                order.PaymentDate = DateTime.UtcNow;

                var orderItems = _context.OrderItems.Where(oi => oi.OrderId == order.OrderId).ToList();
                foreach (var item in orderItems)
                {
                    var book = _context.Books.Find(item.BookId);
                    if (book != null && book.TotalCopies >= item.Quantity)
                    {
                        book.TotalCopies -= item.Quantity;
                    }
                }

                var cart = _context.Carts.FirstOrDefault(c => c.UserId == order.UserId);
                if (cart != null)
                {
                    var cartItems = _context.CartItems.Where(ci => ci.CartId == cart.CartId);
                    _context.CartItems.RemoveRange(cartItems);
                }

                _context.SaveChanges();

                _context.AuditLogs.Add(new AuditLog
                {
                    AuditLogId = Guid.NewGuid(),
                    ActorUserId = order.UserId,
                    ActorRole = "Student",
                    Action = "Order Placed",
                    EntityName = "Order",
                    EntityId = order.OrderId,
                    Description = $"Order #{transactionId} placed successfully. Amount: ৳{order.TotalAmount}"
                });
                _context.SaveChanges();

                NotificationHelper.Send(
                    _context,
                    order.UserId,
                    "Order Confirmed!",
                    $"Your order #{transactionId} has been confirmed. Total: ৳{order.TotalAmount:N0}",
                    "success");

                var librarianIds = _context.Users
                    .Where(u => _context.Roles.Any(r => r.RoleId == u.RoleId && r.RoleName == "Librarian"))
                    .Select(u => u.UserId)
                    .ToList();

                foreach (var libId in librarianIds)
                {
                    NotificationHelper.Send(
                        _context,
                        libId,
                        "New Order Received",
                        $"Order #{transactionId} received. Amount: ৳{order.TotalAmount:N0}. Ready to pack.",
                        "info");
                }
            }

            return RedirectToAction("Complete", new { transactionId, status = "success" });
        }

        [HttpPost]
        public IActionResult Fail()
        {
            var transactionId = Request.Form["tran_id"].ToString();
            var order = _context.Orders.FirstOrDefault(o => o.TransactionId == transactionId);

            if (order != null)
            {
                order.PaymentStatus = "Failed";
                order.OrderStatus = "Cancelled";
                _context.SaveChanges();

                NotificationHelper.Send(
                    _context,
                    order.UserId,
                    "Payment Failed",
                    $"Payment for order #{transactionId} failed. Please try again.",
                    "error");
            }

            return RedirectToAction("Complete", new { transactionId, status = "fail" });
        }

        [HttpPost]
        public IActionResult Cancel()
        {
            var transactionId = Request.Form["tran_id"].ToString();
            var order = _context.Orders.FirstOrDefault(o => o.TransactionId == transactionId);

            if (order != null)
            {
                order.PaymentStatus = "Cancelled";
                order.OrderStatus = "Cancelled";
                _context.SaveChanges();
            }

            return RedirectToAction("Complete", new { transactionId, status = "cancel" });
        }

        [HttpPost]
        public async Task<IActionResult> IPN()
        {
            var transactionId = Request.Form["tran_id"].ToString();
            var validatorId = Request.Form["val_id"].ToString();
            var status = Request.Form["status"].ToString();

            if (status != "VALID")
                return Ok();

            var storeId = _configuration["SSLCommerz:StoreId"] ?? "";
            var storePassword = _configuration["SSLCommerz:StorePassword"] ?? "";
            var validationApiUrl = _configuration["SSLCommerz:ValidationApiUrl"] ?? "";

            var validationUrl = $"{validationApiUrl}?val_id={validatorId}&store_id={storeId}&store_passwd={storePassword}&format=json";

            var response = await _httpClient.GetAsync(validationUrl);
            var responseString = await response.Content.ReadAsStringAsync();

            try
            {
                var json = System.Text.Json.JsonDocument.Parse(responseString);
                var validationStatus = json.RootElement.GetProperty("status").GetString();

                if (validationStatus == "VALID" || validationStatus == "VALIDATED")
                {
                    var order = _context.Orders.FirstOrDefault(o => o.TransactionId == transactionId);

                    if (order != null && order.PaymentStatus != "Success")
                    {
                        order.PaymentStatus = "Success";
                        order.OrderStatus = "Confirmed";
                        order.PaymentDate = DateTime.UtcNow;

                        var orderItems = _context.OrderItems.Where(oi => oi.OrderId == order.OrderId).ToList();
                        foreach (var item in orderItems)
                        {
                            var book = _context.Books.Find(item.BookId);
                            if (book != null && book.TotalCopies >= item.Quantity)
                            {
                                book.TotalCopies -= item.Quantity;
                            }
                        }

                        var cart = _context.Carts.FirstOrDefault(c => c.UserId == order.UserId);
                        if (cart != null)
                        {
                            var cartItems = _context.CartItems.Where(ci => ci.CartId == cart.CartId);
                            _context.CartItems.RemoveRange(cartItems);
                        }

                        _context.SaveChanges();
                    }
                }
            }
            catch
            {
                // Log error
            }

            return Ok();
        }

        public IActionResult Complete(string transactionId, string status)
        {
            ViewBag.TransactionId = transactionId;
            ViewBag.Status = status;

            var order = _context.Orders.FirstOrDefault(o => o.TransactionId == transactionId);

            if (order != null)
            {
                ViewBag.OrderId = order.OrderId;
                ViewBag.TotalAmount = order.TotalAmount;
                ViewBag.OrderDate = order.OrderDate;

                // FIX: Use oi.BookTitle instead of oi.Title
                var items = (from oi in _context.OrderItems
                             where oi.OrderId == order.OrderId
                             select new
                             {
                                 BookTitle = oi.BookTitle,  // CHANGED FROM Title
                                 oi.Price,
                                 oi.Quantity
                             }).ToList();

                ViewBag.OrderItems = items;
            }

            return View();
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