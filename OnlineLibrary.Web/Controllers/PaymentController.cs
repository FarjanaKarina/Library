using Microsoft.AspNetCore.Mvc;
using OnlineLibrary.Infrastructure.Data;
using OnlineLibrary.Infrastructure.Domain.Entities;
using OnlineLibrary.Infrastructure.Helpers;

namespace OnlineLibrary.Web.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // PAYMENT PAGE
        // =========================
        public IActionResult Pay(string purpose, decimal amount)
        {
            if (HttpContext.Session.GetString("UserId") == null)
                return RedirectToAction("Login", "Account");

            ViewBag.Purpose = purpose;
            ViewBag.Amount = amount;

            return View();
        }

        // =========================
        // CONFIRM PAYMENT
        // =========================
        [HttpPost]
        public IActionResult Confirm(string purpose, decimal amount)
        {
            var userId = Guid.Parse(HttpContext.Session.GetString("UserId"));

            var payment = new Payment
            {
                PaymentId = Guid.NewGuid(),
                UserId = userId,
                Purpose = purpose,
                Amount = amount,
                IsPaid = true
            };

            _context.Payments.Add(payment);
            _context.SaveChanges();

            // 🔔 NOTIFY USER
            NotificationHelper.Send(
                _context,
                userId,
                "Payment Successful",
                $"{purpose} payment completed successfully.",
                "success");

            return RedirectToAction("Dashboard", "Student");
        }
    }
}
