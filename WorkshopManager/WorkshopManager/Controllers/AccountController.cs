using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using WorkshopManager.ViewModels;
using WorkshopManager.ViewModels.Account;

namespace WorkshopManager.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToLocal(returnUrl);
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                try
                {
                    var result = await _signInManager.PasswordSignInAsync(
                        model.Email,
                        model.Password,
                        model.RememberMe,
                        lockoutOnFailure: true);

                    if (result.Succeeded)
                    {
                        var user = await _userManager.FindByEmailAsync(model.Email);
                        var roles = await _userManager.GetRolesAsync(user!);

                        _logger.LogInformation("Użytkownik {Email} zalogował się pomyślnie. Role: {Roles}",
                            model.Email, string.Join(", ", roles));

                        return RedirectToLocal(returnUrl);
                    }

                    if (result.IsLockedOut)
                    {
                        _logger.LogWarning("Konto użytkownika {Email} zostało zablokowane", model.Email);
                        return RedirectToAction(nameof(Lockout));
                    }

                    _logger.LogWarning("Nieudana próba logowania dla użytkownika {Email}", model.Email);
                    ModelState.AddModelError(string.Empty, "Nieprawidłowy email lub hasło.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Błąd podczas logowania użytkownika {Email}", model.Email);
                    ModelState.AddModelError(string.Empty, "Wystąpił błąd podczas logowania. Spróbuj ponownie.");
                }
            }

            return View(model);
        }

        // GET: /Account/Register
        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Register()
        {
            var availableRoles = _roleManager.Roles
                .Select(r => r.Name)
                .Where(r => r != null)
                .Cast<string>()
                .ToList();

            var model = new RegisterViewModel
            {
                AvailableRoles = availableRoles
            };

            return View(model);
        }

        // POST: /Account/Register
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = new IdentityUser
                    {
                        UserName = model.Email,
                        Email = model.Email,
                        EmailConfirmed = true
                    };

                    var result = await _userManager.CreateAsync(user, model.Password);

                    if (result.Succeeded)
                    {
                        if (!string.IsNullOrEmpty(model.Role))
                        {
                            await _userManager.AddToRoleAsync(user, model.Role);
                        }

                        _logger.LogInformation("Administrator {AdminEmail} utworzył nowe konto dla użytkownika {UserEmail} z rolą {Role}",
                            User.Identity?.Name, user.Email, model.Role);

                        TempData["SuccessMessage"] = $"Konto dla użytkownika {user.Email} zostało utworzone pomyślnie.";
                        return RedirectToAction(nameof(Register));
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Błąd podczas tworzenia konta dla użytkownika {Email}", model.Email);
                    ModelState.AddModelError(string.Empty, "Wystąpił błąd podczas tworzenia konta. Spróbuj ponownie.");
                }
            }

            // Odświeżenie listy ról w przypadku błędu
            model.AvailableRoles = _roleManager.Roles
                .Select(r => r.Name)
                .Where(r => r != null)
                .Cast<string>()
                .ToList();

            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userEmail = User.Identity?.Name;
            await _signInManager.SignOutAsync();

            _logger.LogInformation("Użytkownik {Email} wylogował się", userEmail);

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied(string? returnUrl = null)
        {
            _logger.LogWarning("Dostęp zabroniony dla użytkownika {UserName} do {Url}",
                User.Identity?.Name, returnUrl ?? "nieznany URL");

            var model = new AccessDeniedViewModel
            {
                ReturnUrl = returnUrl,
                ErrorMessage = "Nie masz uprawnień do wykonania tej akcji.",
                RequiredRole = "Administrator lub Recepcjonista",
                UserRole = User.Claims.FirstOrDefault(c => c.Type.Contains("role"))?.Value,
                Action = HttpContext.Request.RouteValues["action"]?.ToString(),
                Controller = HttpContext.Request.RouteValues["controller"]?.ToString()
            };

            return View(model);
        }

        // GET: /Account/Lockout
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Lockout()
        {
            var model = new LockoutViewModel();

            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                    model.LockoutEnd = lockoutEnd?.DateTime;
                }
            }

            return View(model);
        }

        // GET: /Account/Profile
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
            var roles = await _userManager.GetRolesAsync(user);

            var model = new UserProfileViewModel
            {
                Id = user.Id,
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber,
                Role = roles.FirstOrDefault(),
                EmailConfirmed = user.EmailConfirmed,
                LockoutEnabled = user.LockoutEnabled,
                LockoutEnd = lockoutEnd?.DateTime
            };

            return View(model);
        }

        // GET: /Account/ChangePassword
        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                _logger.LogInformation("Użytkownik {Email} zmienił hasło", user.Email);

                TempData["SuccessMessage"] = "Hasło zostało zmienione pomyślnie.";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        #region Helpers

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        #endregion
    }
}