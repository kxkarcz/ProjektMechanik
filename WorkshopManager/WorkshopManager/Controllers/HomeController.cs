using Microsoft.AspNetCore.Mvc;
using WorkshopManager.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using WorkshopManager.Extensions;
using System.Security.Claims;
using WorkshopManager.Models;

namespace WorkshopManager.Controllers
{
    [Authorize(Policy = "WorkshopUser")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Rozpoczęcie ładowania strony głównej - dashboard");

            try
            {
                _logger.LogDebug("Pobieranie statystyk zleceń serwisowych");
                var totalOrders = await _context.ServiceOrders.CountAsync();

                _logger.LogDebug("Pobieranie statystyk aktywnych zleceń");
                var activeOrders = await _context.ServiceOrders
                    .Where(o => o.Status != "Zakończone").CountAsync();

                _logger.LogDebug("Pobieranie statystyk klientów");
                var totalCustomers = await _context.Customers.CountAsync();

                _logger.LogDebug("Pobieranie statystyk pojazdów");
                var totalVehicles = await _context.Vehicles.CountAsync();

                ViewBag.TotalOrders = totalOrders;
                ViewBag.ActiveOrders = activeOrders;
                ViewBag.TotalCustomers = totalCustomers;
                ViewBag.TotalVehicles = totalVehicles;

                _logger.LogInformation("Pomyślnie załadowano dashboard. Statystyki: {TotalOrders} zleceń (w tym {ActiveOrders} aktywnych), {TotalCustomers} klientów, {TotalVehicles} pojazdów",
                    totalOrders, activeOrders, totalCustomers, totalVehicles);
                var completedOrders = totalOrders - activeOrders;
                var completionRate = totalOrders > 0 ? Math.Round((double)completedOrders / totalOrders * 100, 1) : 0;
                var vehiclesPerCustomer = totalCustomers > 0 ? Math.Round((double)totalVehicles / totalCustomers, 1) : 0;

                _logger.LogInformation("Metryki biznesowe: {CompletionRate}% zleceń zakończonych, średnio {VehiclesPerCustomer} pojazdów na klienta",
                    completionRate, vehiclesPerCustomer);

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas ładowania statystyk na stronie głównej");

                ViewBag.TotalOrders = 0;
                ViewBag.ActiveOrders = 0;
                ViewBag.TotalCustomers = 0;
                ViewBag.TotalVehicles = 0;

                TempData["ErrorMessage"] = "Wystąpił błąd podczas ładowania statystyk. Spróbuj odświeżyć stronę.";

                return View();
            }
        }

        public IActionResult Privacy()
        {
            _logger.LogInformation("Użytkownik wszedł na stronę prywatności");
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            _logger.LogWarning("Przekierowanie na stronę błędu - Request ID: {RequestId}",
                Activity.Current?.Id ?? HttpContext.TraceIdentifier);

            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}