using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WorkshopManager.Data;
using WorkshopManager.DTOs;
using WorkshopManager.Mappers;
using WorkshopManager.Models;
using WorkshopManager.Services;
using WorkshopManager.Extensions;
using NLog;
using NLog.Web;

var logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Konfiguracja NLog
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // Add services to the container.
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                           ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();

    // Konfiguracja Identity
    builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 6;

        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;

        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

    // Konfiguracja ciasteczek
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ReturnUrlParameter = "returnUrl";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

    // Autoryzacja
    builder.Services.AddAuthorization(options =>
    {
        options.AddWorkshopPolicies();
    });

    builder.Services.AddControllersWithViews();

    // Rejestracja mapperów i serwisów (Twoje serwisy)
    builder.Services.AddScoped<CommentMapper>();
    builder.Services.AddScoped<ICommentService, CommentService>();

    builder.Services.AddScoped<VehicleMapper>();
    builder.Services.AddScoped<IVehicleService, VehicleService>();

    builder.Services.AddScoped<UsedPartMapper>();
    builder.Services.AddScoped<IUsedPartService, UsedPartService>();

    builder.Services.AddScoped<PartMapper>();
    builder.Services.AddScoped<IPartService, PartService>();

    builder.Services.AddScoped<ServiceOrderMapper>();
    builder.Services.AddScoped<IServiceOrderService, ServiceOrderService>();

    builder.Services.AddScoped<ServiceTaskMapper>();
    builder.Services.AddScoped<IServiceTaskService, ServiceTaskService>();

    builder.Services.AddScoped<ICustomerService, CustomerService>();

    builder.Services.AddScoped<IPdfReportService, PdfReportService>();
    builder.Services.AddSingleton<EmailSenderService>();
    builder.Services.AddHostedService<OpenOrderReportBackgroundService>();

    builder.Services.AddScoped<IDashboardService, DashboardService>();

    // Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "WorkshopManager API",
            Version = "v1",
            Description = "API systemu zarządzania warsztatem samochodowym"
        });

        c.MapType<IFormFile>(() => new Microsoft.OpenApi.Models.OpenApiSchema
        {
            Type = "string",
            Format = "binary"
        });

        c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme",
            Name = "Authorization",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
        });
    });

    var app = builder.Build();

    // Asynchroniczna inicjalizacja baz danych i ról
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();

        await RoleSeeder.SeedRolesAsync(scope.ServiceProvider);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        async Task EnsureUserAsync(string email, string password, string role)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                    logger.Info($"Utworzono konto testowe: {email} / {password} (rola: {role})");
                }
                else
                {
                    logger.Error($"Błąd przy tworzeniu konta {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                if (!await userManager.IsInRoleAsync(user, role))
                {
                    await userManager.AddToRoleAsync(user, role);
                    logger.Info($"Przypisano rolę {role} do użytkownika {email}");
                }
            }
        }

        await EnsureUserAsync("recepcja@workshop.pl", "Recepcja123!", "Recepcjonista");
        await EnsureUserAsync("mechanik@workshop.pl", "Mechanik123!", "Mechanik");
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseMigrationsEndPoint();

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "WorkshopManager API v1");
            c.RoutePrefix = "swagger";
        });

        logger.Info("Aplikacja uruchomiona w trybie deweloperskim");
        logger.Info("Swagger UI dostępny pod adresem: /swagger");
        logger.Info("Domyślne konto administratora: admin@workshop.pl / Admin123!");
        logger.Info("Logowanie dostępne pod adresem: /Account/Login");
    }
    else
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();

        logger.Info("Aplikacja uruchomiona w trybie produkcyjnym");
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.MapControllers();

    // Testowe API (tylko dev)
    if (app.Environment.IsDevelopment())
    {
        app.MapGet("/api/test/customers", async (ICustomerService svc) =>
            Results.Ok(await svc.GetAllAsync(null)))
            .RequireAuthorization("AdminOrRecepcjonista")
            .WithTags("Test API");

        app.MapPost("/api/test/customers", async (ICustomerService svc) =>
        {
            var dto = new CustomerDto
            {
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com",
                PhoneNumber = "123456789"
            };
            await svc.CreateAsync(dto);
            return Results.Created($"/api/test/customers/{dto.Id}", dto);
        })
        .RequireAuthorization("AdminOrRecepcjonista")
        .WithTags("Test API");

        app.MapGet("/api/test/user-info", (HttpContext context) =>
        {
            var user = context.User;
            return Results.Ok(new
            {
                IsAuthenticated = user.Identity?.IsAuthenticated ?? false,
                Name = user.Identity?.Name,
                Roles = user.Claims
                    .Where(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                    .Select(c => c.Value)
                    .ToList(),
                Claims = user.Claims.Select(c => new { c.Type, c.Value }).ToList()
            });
        })
        .RequireAuthorization()
        .WithTags("Test API");
    }

    // Middleware do logowania żądań (opcjonalne)
    app.Use(async (context, next) =>
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            logger.Debug("Żądanie: {Method} {Path} od użytkownika: {User}",
                context.Request.Method,
                context.Request.Path,
                context.User.Identity.Name);
        }
        await next();
    });

    logger.Info("Aplikacja WorkshopManager została uruchomiona pomyślnie");
    logger.Info("Dostępne role: Admin, Mechanik, Recepcjonista");

    await app.RunAsync();
}
catch (Exception ex)
{
    logger.Error(ex, "Krytyczny błąd - aplikacja WorkshopManager nie mogła się uruchomić");
    throw;
}
finally
{
    LogManager.Shutdown();
}
