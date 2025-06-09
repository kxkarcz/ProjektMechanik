using System.Security.Claims;
using WorkshopManager.ViewModels;

namespace WorkshopManager.Services
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardDataAsync(ClaimsPrincipal user);
    }
}
