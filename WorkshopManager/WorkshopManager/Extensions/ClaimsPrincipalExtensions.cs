using System.Security.Claims;

namespace WorkshopManager.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static bool IsAuthenticated(this ClaimsPrincipal user)
        {
            return user?.Identity?.IsAuthenticated ?? false;
        }

        public static string GetUserName(this ClaimsPrincipal user)
        {
            return user?.Identity?.Name ?? "";
        }

        public static string GetUserId(this ClaimsPrincipal user)
        {
            return user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        }
        public static string GetUserEmail(this ClaimsPrincipal user)
        {
            return user?.FindFirst(ClaimTypes.Email)?.Value ??
                   user?.FindFirst(ClaimTypes.Name)?.Value ?? "";
        }

        public static string GetUserRole(this ClaimsPrincipal user)
        {
            var roleClaim = user?.FindFirst(ClaimTypes.Role)?.Value ??
                           user?.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;

            return roleClaim ?? "User";
        }

        public static List<string> GetUserRoles(this ClaimsPrincipal user)
        {
            if (user == null) return new List<string>();

            return user.Claims
                .Where(c => c.Type == ClaimTypes.Role ||
                           c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                .Select(c => c.Value)
                .ToList();
        }
        public static bool IsAdmin(this ClaimsPrincipal user)
        {
            return user.IsInRole("Admin") || user.IsInRole("Administrator");
        }

        public static bool IsMechanic(this ClaimsPrincipal user)
        {
            return user.IsInRole("Mechanik") || user.IsInRole("Mechanic");
        }

        public static bool IsReceptionist(this ClaimsPrincipal user)
        {
            return user.IsInRole("Recepcjonista") || user.IsInRole("Receptionist");
        }

        public static bool CanManageUsers(this ClaimsPrincipal user)
        {
            return user.IsAdmin();
        }

        public static bool CanManageCustomers(this ClaimsPrincipal user)
        {
            return user.IsAdmin() || user.IsReceptionist();
        }

        public static bool CanManageVehicles(this ClaimsPrincipal user)
        {
            return user.IsAdmin() || user.IsReceptionist();
        }

        public static bool CanCreateOrders(this ClaimsPrincipal user)
        {
            return user.IsMechanic() || user.IsReceptionist() || user.IsAdmin();
        }

        public static bool CanEditOrders(this ClaimsPrincipal user)
        {
            return user.IsMechanic() || user.IsReceptionist() || user.IsAdmin();
        }

        public static bool CanDeleteOrders(this ClaimsPrincipal user)
        {
            return user.IsAdmin() || user.IsReceptionist();
        }

        public static bool CanManageParts(this ClaimsPrincipal user)
        {
            return user.IsAdmin() || user.IsReceptionist();
        }

        public static bool CanAddComments(this ClaimsPrincipal user)
        {
            return user.IsAuthenticated();
        }

        public static bool CanDeleteComments(this ClaimsPrincipal user)
        {
            return user.IsAdmin() || user.IsReceptionist();
        }

        public static bool CanGenerateReports(this ClaimsPrincipal user)
        {
            return user.IsAuthenticated();
        }

        public static bool CanGenerateAdvancedReports(this ClaimsPrincipal user)
        {
            return user.IsAdmin() || user.IsReceptionist();
        }
        public static bool CanEditOrder(this ClaimsPrincipal user, string? assignedMechanicId)
        {
            if (user.IsAdmin() || user.IsReceptionist())
                return true;

            if (user.IsMechanic())
            {
                var userId = user.GetUserId();
                return !string.IsNullOrEmpty(userId) && userId == assignedMechanicId;
            }

            return false;
        }

        public static bool CanViewOrder(this ClaimsPrincipal user, string? assignedMechanicId)
        {
            if (user.IsAdmin() || user.IsReceptionist())
                return true;

            if (user.IsMechanic())
            {
                var userId = user.GetUserId();
                return !string.IsNullOrEmpty(userId) && userId == assignedMechanicId;
            }

            return false;
        }

        public static bool CanDeleteOrder(this ClaimsPrincipal user, string? assignedMechanicId)
        {
            return user.IsAdmin() || user.IsReceptionist();
        }

        public static bool CanAddCommentToOrder(this ClaimsPrincipal user, string? assignedMechanicId)
        {
            if (user.IsAdmin() || user.IsReceptionist())
                return true;

            if (user.IsMechanic())
            {
                var userId = user.GetUserId();
                return !string.IsNullOrEmpty(userId) && userId == assignedMechanicId;
            }

            return false;
        }

        public static bool HasRole(this ClaimsPrincipal user, string role)
        {
            if (string.IsNullOrEmpty(role)) return false;

            var userRoles = user.GetUserRoles();
            return userRoles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
        }

        public static bool HasAnyRole(this ClaimsPrincipal user, params string[] roles)
        {
            if (roles == null || roles.Length == 0) return false;

            var userRoles = user.GetUserRoles();
            return roles.Any(role => userRoles.Any(ur =>
                string.Equals(ur, role, StringComparison.OrdinalIgnoreCase)));
        }

        public static bool HasAllRoles(this ClaimsPrincipal user, params string[] roles)
        {
            if (roles == null || roles.Length == 0) return true;

            var userRoles = user.GetUserRoles();
            return roles.All(role => userRoles.Any(ur =>
                string.Equals(ur, role, StringComparison.OrdinalIgnoreCase)));
        }

        public static List<string> GetUserPermissions(this ClaimsPrincipal user)
        {
            var permissions = new List<string>();

            if (user.CanManageUsers()) permissions.Add("Zarządzanie użytkownikami");
            if (user.CanManageCustomers()) permissions.Add("Zarządzanie klientami");
            if (user.CanManageVehicles()) permissions.Add("Zarządzanie pojazdami");
            if (user.CanCreateOrders()) permissions.Add("Tworzenie zleceń");
            if (user.CanEditOrders()) permissions.Add("Edycja zleceń");
            if (user.CanDeleteOrders()) permissions.Add("Usuwanie zleceń");
            if (user.CanManageParts()) permissions.Add("Zarządzanie częściami");
            if (user.CanAddComments()) permissions.Add("Dodawanie komentarzy");
            if (user.CanDeleteComments()) permissions.Add("Usuwanie komentarzy");
            if (user.CanGenerateReports()) permissions.Add("Generowanie raportów");
            if (user.CanGenerateAdvancedReports()) permissions.Add("Zaawansowane raporty");

            return permissions;
        }

        public static Dictionary<string, object> GetUserInfo(this ClaimsPrincipal user)
        {
            return new Dictionary<string, object>
            {
                ["IsAuthenticated"] = user.IsAuthenticated(),
                ["UserName"] = user.GetUserName(),
                ["UserId"] = user.GetUserId(),
                ["Email"] = user.GetUserEmail(),
                ["Role"] = user.GetUserRole(),
                ["Roles"] = user.GetUserRoles(),
                ["Permissions"] = user.GetUserPermissions(),
                ["IsAdmin"] = user.IsAdmin(),
                ["IsMechanic"] = user.IsMechanic(),
                ["IsReceptionist"] = user.IsReceptionist()
            };
        }
    }
}