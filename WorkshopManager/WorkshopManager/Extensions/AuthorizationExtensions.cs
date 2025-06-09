using Microsoft.AspNetCore.Authorization;

namespace WorkshopManager.Extensions
{
    public static class AuthorizationExtensions
    {
        public static void AddWorkshopPolicies(this AuthorizationOptions options)
        {
            // Polityka dla administratorów
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("Admin"));

            // Polityka dla mechaników
            options.AddPolicy("MechanikOnly", policy =>
                policy.RequireRole("Mechanik"));

            // Polityka dla recepcjonistów
            options.AddPolicy("RecepcionistaOnly", policy =>
                policy.RequireRole("Recepcjonista"));

            // Polityka dla administratorów i recepcjonistów (zarządzanie klientami, częściami)
            options.AddPolicy("AdminOrRecepcjonista", policy =>
                policy.RequireRole("Admin", "Recepcjonista"));

            // Polityka dla wszystkich zalogowanych użytkowników warsztatu
            options.AddPolicy("WorkshopUser", policy =>
                policy.RequireRole("Admin", "Mechanik", "Recepcjonista"));
        }
    }
}