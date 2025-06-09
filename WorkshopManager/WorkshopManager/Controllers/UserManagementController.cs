using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkshopManager.ViewModels;
using WorkshopManager.Extensions;
using System.Security.Claims;
using WorkshopManager.Models;
using WorkshopManager.ViewModels.Account;

namespace WorkshopManager.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class UserManagementController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<UserManagementController> _logger;

        public UserManagementController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<UserManagementController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        // GET: UserManagement
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Rozpoczęcie pobierania listy wszystkich użytkowników");

            try
            {
                var users = await _userManager.Users.ToListAsync();
                var userViewModels = new List<UserListViewModel>();

                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    userViewModels.Add(new UserListViewModel
                    {
                        Id = user.Id,
                        Email = user.Email ?? "",
                        UserName = user.UserName ?? "",
                        Roles = roles.ToList(),
                        EmailConfirmed = user.EmailConfirmed
                    });
                }

                _logger.LogInformation("Pobrano {UserCount} użytkowników", userViewModels.Count);
                return View(userViewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania listy użytkowników");
                TempData["ErrorMessage"] = "Wystąpił błąd podczas pobierania listy użytkowników.";
                return View(new List<UserListViewModel>());
            }
        }

        // GET: UserManagement/Create
        public IActionResult Create()
        {
            _logger.LogInformation("Wyświetlanie formularza tworzenia nowego użytkownika");
            return View(new CreateUserViewModel());
        }

        // POST: UserManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            _logger.LogInformation("Rozpoczęcie tworzenia nowego użytkownika: {Email}, Rola: {Role}",
                model.Email, model.SelectedRole);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model użytkownika nie jest prawidłowy: {Email}", model.Email);
                return View(model);
            }

            try
            {
                // Sprawdzenie czy rola istnieje
                if (!await _roleManager.RoleExistsAsync(model.SelectedRole))
                {
                    _logger.LogError("Próba przypisania nieistniejącej roli: {Role}", model.SelectedRole);
                    ModelState.AddModelError("SelectedRole", "Wybrana rola nie istnieje");
                    return View(model);
                }

                // Utworzenie użytkownika
                var user = new IdentityUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    EmailConfirmed = true // Auto-potwierdzenie dla użytkowników utworzonych przez admina
                };

                var createResult = await _userManager.CreateAsync(user, model.Password);
                if (createResult.Succeeded)
                {
                    _logger.LogInformation("Pomyślnie utworzono użytkownika: {Email}", model.Email);

                    // Przypisanie roli
                    var roleResult = await _userManager.AddToRoleAsync(user, model.SelectedRole);
                    if (roleResult.Succeeded)
                    {
                        _logger.LogInformation("Przypisano rolę {Role} użytkownikowi: {Email}",
                            model.SelectedRole, model.Email);

                        TempData["SuccessMessage"] = $"Użytkownik {model.Email} został pomyślnie utworzony z rolą {model.SelectedRole}.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        _logger.LogError("Błąd podczas przypisywania roli {Role} użytkownikowi {Email}: {Errors}",
                            model.SelectedRole, model.Email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));

                        // Usunięcie użytkownika jeśli nie udało się przypisać roli
                        await _userManager.DeleteAsync(user);

                        foreach (var error in roleResult.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                    }
                }
                else
                {
                    _logger.LogError("Błąd podczas tworzenia użytkownika {Email}: {Errors}",
                        model.Email, string.Join(", ", createResult.Errors.Select(e => e.Description)));

                    foreach (var error in createResult.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas tworzenia użytkownika: {Email}", model.Email);
                ModelState.AddModelError("", "Wystąpił nieoczekiwany błąd podczas tworzenia użytkownika.");
            }

            return View(model);
        }

        // GET: UserManagement/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            _logger.LogInformation("Wyświetlanie formularza edycji użytkownika ID: {UserId}", id);

            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("Nie podano ID użytkownika do edycji");
                return NotFound();
            }

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("Nie znaleziono użytkownika o ID: {UserId}", id);
                    return NotFound();
                }

                var roles = await _userManager.GetRolesAsync(user);
                var model = new EditUserViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? "",
                    CurrentRoles = roles.ToList(),
                    SelectedRole = roles.FirstOrDefault() ?? "",
                    IsLocked = await _userManager.IsLockedOutAsync(user)
                };

                _logger.LogInformation("Załadowano dane użytkownika do edycji: {Email}, Role: {Roles}",
                    user.Email, string.Join(", ", roles));

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas ładowania danych użytkownika do edycji: {UserId}", id);
                return NotFound();
            }
        }

        // POST: UserManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            _logger.LogInformation("Rozpoczęcie aktualizacji użytkownika ID: {UserId}", model.Id);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model edycji użytkownika nie jest prawidłowy: {UserId}", model.Id);
                return View(model);
            }

            try
            {
                var user = await _userManager.FindByIdAsync(model.Id);
                if (user == null)
                {
                    _logger.LogWarning("Nie znaleziono użytkownika o ID: {UserId} do aktualizacji", model.Id);
                    return NotFound();
                }

                var oldEmail = user.Email;
                var currentRoles = await _userManager.GetRolesAsync(user);

                // Aktualizacja email
                if (user.Email != model.Email)
                {
                    user.Email = model.Email;
                    user.UserName = model.Email;
                    var updateResult = await _userManager.UpdateAsync(user);

                    if (!updateResult.Succeeded)
                    {
                        _logger.LogError("Błąd podczas aktualizacji email użytkownika {UserId}: {Errors}",
                            model.Id, string.Join(", ", updateResult.Errors.Select(e => e.Description)));

                        foreach (var error in updateResult.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                        return View(model);
                    }
                }

                // Aktualizacja roli jeśli się zmieniła
                if (!currentRoles.Contains(model.SelectedRole))
                {
                    // Usunięcie starych ról
                    if (currentRoles.Any())
                    {
                        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                        if (!removeResult.Succeeded)
                        {
                            _logger.LogError("Błąd podczas usuwania starych ról użytkownika {UserId}: {Errors}",
                                model.Id, string.Join(", ", removeResult.Errors.Select(e => e.Description)));
                        }
                    }

                    // Dodanie nowej roli
                    var addRoleResult = await _userManager.AddToRoleAsync(user, model.SelectedRole);
                    if (!addRoleResult.Succeeded)
                    {
                        _logger.LogError("Błąd podczas dodawania nowej roli {Role} użytkownikowi {UserId}: {Errors}",
                            model.SelectedRole, model.Id, string.Join(", ", addRoleResult.Errors.Select(e => e.Description)));

                        foreach (var error in addRoleResult.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                        return View(model);
                    }
                }

                // Zarządzanie blokadą konta
                if (model.IsLocked)
                {
                    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
                    _logger.LogInformation("Zablokowano konto użytkownika: {Email}", user.Email);
                }
                else
                {
                    await _userManager.SetLockoutEndDateAsync(user, null);
                    _logger.LogInformation("Odblokowano konto użytkownika: {Email}", user.Email);
                }

                _logger.LogInformation("Pomyślnie zaktualizowano użytkownika. ID: {UserId}, Email: {OldEmail} -> {NewEmail}, " +
                    "Role: {OldRoles} -> {NewRole}, Zablokowany: {IsLocked}",
                    model.Id, oldEmail, model.Email, string.Join(", ", currentRoles), model.SelectedRole, model.IsLocked);

                TempData["SuccessMessage"] = $"Użytkownik {model.Email} został pomyślnie zaktualizowany.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas aktualizacji użytkownika: {UserId}", model.Id);
                ModelState.AddModelError("", "Wystąpił nieoczekiwany błąd podczas aktualizacji użytkownika.");
                return View(model);
            }
        }

        // GET: UserManagement/ChangePassword/5
        public async Task<IActionResult> ChangePassword(string id)
        {
            _logger.LogInformation("Wyświetlanie formularza zmiany hasła dla użytkownika ID: {UserId}", id);

            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning("Nie znaleziono użytkownika o ID: {UserId} do zmiany hasła", id);
                return NotFound();
            }

            var model = new ChangePasswordViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? ""
            };

            return View(model);
        }

        // POST: UserManagement/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            _logger.LogInformation("Rozpoczęcie zmiany hasła dla użytkownika ID: {UserId}", model.UserId);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = await _userManager.FindByIdAsync(model.UserId);
                if (user == null)
                {
                    return NotFound();
                }

                // Usunięcie starego hasła i ustawienie nowego (admin reset)
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Pomyślnie zmieniono hasło dla użytkownika: {Email}", user.Email);
                    TempData["SuccessMessage"] = $"Hasło dla użytkownika {user.Email} zostało pomyślnie zmienione.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _logger.LogError("Błąd podczas zmiany hasła dla użytkownika {Email}: {Errors}",
                        user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas zmiany hasła dla użytkownika: {UserId}", model.UserId);
                ModelState.AddModelError("", "Wystąpił błąd podczas zmiany hasła.");
            }

            return View(model);
        }

        // POST: UserManagement/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            _logger.LogInformation("Rozpoczęcie usuwania użytkownika ID: {UserId}", id);

            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("Nie znaleziono użytkownika o ID: {UserId} do usunięcia", id);
                    return NotFound();
                }

                // Sprawdzenie czy to nie jedyny admin
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                var userRoles = await _userManager.GetRolesAsync(user);

                if (userRoles.Contains("Admin") && admins.Count <= 1)
                {
                    _logger.LogWarning("Próba usunięcia jedynego administratora: {Email}", user.Email);
                    TempData["ErrorMessage"] = "Nie można usunąć jedynego administratora w systemie.";
                    return RedirectToAction(nameof(Index));
                }

                var email = user.Email;
                var result = await _userManager.DeleteAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Pomyślnie usunięto użytkownika: {Email}", email);
                    TempData["SuccessMessage"] = $"Użytkownik {email} został pomyślnie usunięty.";
                }
                else
                {
                    _logger.LogError("Błąd podczas usuwania użytkownika {Email}: {Errors}",
                        email, string.Join(", ", result.Errors.Select(e => e.Description)));
                    TempData["ErrorMessage"] = "Wystąpił błąd podczas usuwania użytkownika.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas usuwania użytkownika: {UserId}", id);
                TempData["ErrorMessage"] = "Wystąpił nieoczekiwany błąd podczas usuwania użytkownika.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}