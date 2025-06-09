namespace WorkshopManager.ViewModels
{
    public class DashboardViewModel
    {
        public DashboardStatistics Statistics { get; set; } = new();
        public List<RecentActivity> RecentActivities { get; set; } = new();
        public List<QuickAction> QuickActions { get; set; } = new();
        public UserInfo CurrentUser { get; set; } = new();
    }

    public class DashboardStatistics
    {
        public int TotalOrders { get; set; }
        public int ActiveOrders { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalVehicles { get; set; }
        public decimal CompletionRate { get; set; }
        public decimal VehiclesPerCustomer { get; set; }

        // Statystyki dla mechaników
        public int MyActiveOrders { get; set; }
        public int MyCompletedOrdersThisMonth { get; set; }

        // Statystyki dla adminów
        public int TotalUsers { get; set; }
        public int NewOrdersToday { get; set; }
    }

    public class RecentActivity
    {
        public string Icon { get; set; } = null!;
        public string Description { get; set; } = null!;
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } = null!; // "Order", "Customer", "Vehicle", itp.
        public string? Url { get; set; }
    }

    public class QuickAction
    {
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Icon { get; set; } = null!;
        public string Url { get; set; } = null!;
        public string ButtonClass { get; set; } = "btn-primary";
        public bool RequiresRole { get; set; }
        public string[] AllowedRoles { get; set; } = Array.Empty<string>();
    }

    public class UserInfo
    {
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
        public List<string> Permissions { get; set; } = new();
        public DateTime LastLogin { get; set; }
    }
}