using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WorkshopManager.Data;
using WorkshopManager.Extensions;
using WorkshopManager.ViewModels;
using System.Security.Claims;
using WorkshopManager.Extensions;

namespace WorkshopManager.Services
{

    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            ILogger<DashboardService> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync(ClaimsPrincipal user)
        {
            try
            {
                _logger.LogInformation("Generowanie danych dashboard dla użytkownika: {UserName}", user.Identity?.Name);

                var viewModel = new DashboardViewModel
                {
                    Statistics = await GetStatisticsAsync(user),
                    RecentActivities = await GetRecentActivitiesAsync(user),
                    QuickActions = GetQuickActions(user),
                    CurrentUser = await GetCurrentUserInfoAsync(user)
                };

                _logger.LogInformation("Dashboard wygenerowany pomyślnie dla użytkownika: {UserName}", user.Identity?.Name);
                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas generowania dashboard dla użytkownika: {UserName}", user.Identity?.Name);
                throw;
            }
        }

        private async Task<DashboardStatistics> GetStatisticsAsync(ClaimsPrincipal user)
        {
            var stats = new DashboardStatistics();

            // Podstawowe statystyki dla wszystkich
            stats.TotalOrders = await _context.ServiceOrders.CountAsync();
            stats.ActiveOrders = await _context.ServiceOrders.CountAsync(o => o.Status != "Zakończone");

            if (user.CanManageCustomers())
            {
                stats.TotalCustomers = await _context.Customers.CountAsync();
                stats.TotalVehicles = await _context.Vehicles.CountAsync();
                stats.VehiclesPerCustomer = stats.TotalCustomers > 0
                    ? Math.Round((decimal)stats.TotalVehicles / stats.TotalCustomers, 1)
                    : 0;
            }

            var completedOrders = stats.TotalOrders - stats.ActiveOrders;
            stats.CompletionRate = stats.TotalOrders > 0
                ? Math.Round((decimal)completedOrders / stats.TotalOrders * 100, 1)
                : 0;

            // Statystyki dla mechaników
            if (user.IsMechanic())
            {
                var currentUser = await _userManager.FindByNameAsync(user.Identity?.Name ?? "");
                if (currentUser != null)
                {
                    stats.MyActiveOrders = await _context.ServiceOrders
                        .CountAsync(o => o.AssignedMechanicId == currentUser.Id && o.Status != "Zakończone");

                    var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    stats.MyCompletedOrdersThisMonth = await _context.ServiceOrders
                        .CountAsync(o => o.AssignedMechanicId == currentUser.Id &&
                                        o.Status == "Zakończone");
                }
            }

            // Statystyki dla adminów
            if (user.IsAdmin())
            {
                stats.TotalUsers = await _userManager.Users.CountAsync();
                var today = DateTime.Today;
                stats.NewOrdersToday = await _context.ServiceOrders
                    .CountAsync();
            }

            return stats;
        }

        private async Task<List<RecentActivity>> GetRecentActivitiesAsync(ClaimsPrincipal user)
        {
            var activities = new List<RecentActivity>();

            try
            {
                // Pobieranie ostatnich zleceń
                var recentOrders = await _context.ServiceOrders
                    .OrderByDescending(o => o.Id)
                    .Take(5)
                    .Select(o => new { o.Id, o.Status, o.VehicleId })
                    .ToListAsync();

                foreach (var order in recentOrders)
                {
                    activities.Add(new RecentActivity
                    {
                        Icon = "fas fa-clipboard-list",
                        Description = $"Zlecenie #{order.Id} - Status: {order.Status}",
                        Timestamp = DateTime.Now.AddHours(-activities.Count),
                        Type = "Order",
                        Url = $"/ServiceOrder/Details/{order.Id}"
                    });
                }

                if (user.CanManageCustomers())
                {
                    var recentCustomers = await _context.Customers
                        .OrderByDescending(c => c.Id)
                        .Take(3)
                        .Select(c => new { c.Id, c.FirstName, c.LastName })
                        .ToListAsync();

                    foreach (var customer in recentCustomers)
                    {
                        activities.Add(new RecentActivity
                        {
                            Icon = "fas fa-user-plus",
                            Description = $"Nowy klient: {customer.FirstName} {customer.LastName}",
                            Timestamp = DateTime.Now.AddDays(-activities.Count),
                            Type = "Customer",
                            Url = $"/Customer/Details/{customer.Id}"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania ostatnich aktywności");
            }

            return activities.OrderByDescending(a => a.Timestamp).Take(10).ToList();
        }

        private List<QuickAction> GetQuickActions(ClaimsPrincipal user)
        {
            var actions = new List<QuickAction>();

            actions.Add(new QuickAction
            {
                Title = "Przeglądaj zlecenia",
                Description = "Zobacz wszystkie zlecenia serwisowe",
                Icon = "fas fa-clipboard-list",
                Url = "/ServiceOrder",
                ButtonClass = "btn-primary"
            });

            if (user.CanCreateOrders())
            {
                actions.Add(new QuickAction
                {
                    Title = "Nowe zlecenie",
                    Description = "Utwórz nowe zlecenie serwisowe",
                    Icon = "fas fa-plus-circle",
                    Url = "/ServiceOrder/Create",
                    ButtonClass = "btn-success"
                });
            }

            if (user.CanManageCustomers())
            {
                actions.AddRange(new[]
                {
                    new QuickAction
                    {
                        Title = "Zarządzaj klientami",
                        Description = "Dodaj lub edytuj klientów",
                        Icon = "fas fa-users",
                        Url = "/Customer",
                        ButtonClass = "btn-info"
                    },
                    new QuickAction
                    {
                        Title = "Zarządzaj pojazdami",
                        Description = "Dodaj lub edytuj pojazdy",
                        Icon = "fas fa-car",
                        Url = "/Vehicle",
                        ButtonClass = "btn-secondary"
                    }
                });
            }

            if (user.IsAdmin())
            {
                actions.Add(new QuickAction
                {
                    Title = "Zarządzanie użytkownikami",
                    Description = "Dodaj lub edytuj użytkowników systemu",
                    Icon = "fas fa-users-cog",
                    Url = "/UserManagement",
                    ButtonClass = "btn-danger"
                });
            }

            actions.Add(new QuickAction
            {
                Title = "Generuj raport",
                Description = "Pobierz raport PDF z otwartymi zleceniami",
                Icon = "fas fa-file-pdf",
                Url = "/Report/DownloadOpenOrdersReport",
                ButtonClass = "btn-warning"
            });

            return actions;
        }

        private async Task<UserInfo> GetCurrentUserInfoAsync(ClaimsPrincipal user)
        {
            var userInfo = new UserInfo
            {
                Email = user.Identity?.Name ?? "",
                Role = user.GetUserRole(),
                LastLogin = DateTime.Now
            };

            // Dodanie uprawnień
            if (user.CanManageUsers()) userInfo.Permissions.Add("Zarządzanie użytkownikami");
            if (user.CanManageCustomers()) userInfo.Permissions.Add("Zarządzanie klientami");
            if (user.CanManageVehicles()) userInfo.Permissions.Add("Zarządzanie pojazdami");
            if (user.CanCreateOrders()) userInfo.Permissions.Add("Tworzenie zleceń");
            if (user.CanDeleteOrders()) userInfo.Permissions.Add("Usuwanie zleceń");
            if (user.CanManageParts()) userInfo.Permissions.Add("Zarządzanie częściami");

            return userInfo;
        }
    }
}