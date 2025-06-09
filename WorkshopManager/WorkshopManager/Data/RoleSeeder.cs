using Microsoft.AspNetCore.Identity;

namespace WorkshopManager.Data
{
    public static class RoleSeeder
    {
        public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("RoleSeeder"); 

            try
            {
                logger.LogInformation("Rozpoczęcie inicjalizacji ról systemu");

                // Definicja ról
                string[] roles = { "Admin", "Mechanik", "Recepcjonista" };

                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        logger.LogInformation("Tworzenie roli: {RoleName}", role);
                        var result = await roleManager.CreateAsync(new IdentityRole(role));

                        if (result.Succeeded)
                        {
                            logger.LogInformation("Pomyślnie utworzono rolę: {RoleName}", role);
                        }
                        else
                        {
                            logger.LogError("Błąd podczas tworzenia roli {RoleName}: {Errors}",
                                role, string.Join(", ", result.Errors.Select(e => e.Description)));
                        }
                    }
                    else
                    {
                        logger.LogDebug("Rola {RoleName} już istnieje", role);
                    }
                }

                // Utworzenie domyślnego konta administratora
                await CreateDefaultAdminAsync(userManager, logger);

                logger.LogInformation("Zakończono inicjalizację ról systemu");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Błąd podczas inicjalizacji ról systemu");
                throw;
            }
        }

        private static async Task CreateDefaultAdminAsync(UserManager<IdentityUser> userManager, ILogger logger)
        {
            try
            {
                logger.LogInformation("Sprawdzanie istnienia domyślnego konta administratora");

                const string adminEmail = "admin@workshop.pl";
                const string adminPassword = "Admin123!";

                var adminUser = await userManager.FindByEmailAsync(adminEmail);
                if (adminUser == null)
                {
                    logger.LogInformation("Tworzenie domyślnego konta administratora: {AdminEmail}", adminEmail);

                    adminUser = new IdentityUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(adminUser, adminPassword);
                    if (result.Succeeded)
                    {
                        logger.LogInformation("Pomyślnie utworzono konto administratora: {AdminEmail}", adminEmail);

                        var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
                        if (roleResult.Succeeded)
                        {
                            logger.LogInformation("Przypisano rolę Admin do konta: {AdminEmail}", adminEmail);
                        }
                        else
                        {
                            logger.LogError("Błąd podczas przypisywania roli Admin: {Errors}",
                                string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                        }
                    }
                    else
                    {
                        logger.LogError("Błąd podczas tworzenia konta administratora: {Errors}",
                            string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    logger.LogDebug("Domyślne konto administratora już istnieje: {AdminEmail}", adminEmail);

                    // Sprawdzenie czy admin ma przypisaną rolę
                    if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                    {
                        logger.LogInformation("Przypisywanie roli Admin do istniejącego konta: {AdminEmail}", adminEmail);
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Błąd podczas tworzenia domyślnego konta administratora");
                throw;
            }
        }

        public static async Task SeedTestUsersAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("RoleSeeder");

            try
            {
                logger.LogInformation("Rozpoczęcie tworzenia użytkowników testowych");

                var testUsers = new[]
                {
                    new { Email = "mechanik@workshop.pl", Password = "Mechanik123!", Role = "Mechanik" },
                    new { Email = "recepcjonista@workshop.pl", Password = "Recepcja123!", Role = "Recepcjonista" }
                };

                foreach (var testUser in testUsers)
                {
                    var existingUser = await userManager.FindByEmailAsync(testUser.Email);
                    if (existingUser == null)
                    {
                        logger.LogInformation("Tworzenie użytkownika testowego: {Email} z rolą {Role}",
                            testUser.Email, testUser.Role);

                        var user = new IdentityUser
                        {
                            UserName = testUser.Email,
                            Email = testUser.Email,
                            EmailConfirmed = true
                        };

                        var result = await userManager.CreateAsync(user, testUser.Password);
                        if (result.Succeeded)
                        {
                            await userManager.AddToRoleAsync(user, testUser.Role);
                            logger.LogInformation("Pomyślnie utworzono użytkownika testowego: {Email}", testUser.Email);
                        }
                        else
                        {
                            logger.LogWarning("Nie udało się utworzyć użytkownika testowego {Email}: {Errors}",
                                testUser.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                        }
                    }
                    else
                    {
                        logger.LogDebug("Użytkownik testowy {Email} już istnieje", testUser.Email);
                    }
                }

                logger.LogInformation("Zakończono tworzenie użytkowników testowych");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Błąd podczas tworzenia użytkowników testowych");
                // Nie rzucamy wyjątku - to są tylko użytkownicy testowi
            }
        }
    }
}