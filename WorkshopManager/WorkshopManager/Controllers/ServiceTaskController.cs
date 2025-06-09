using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WorkshopManager.Models;
using WorkshopManager.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using WorkshopManager.Extensions;
using System.Security.Claims;

namespace WorkshopManager.Controllers
{
    [Authorize(Policy = "WorkshopUser")]
    public class ServiceTaskController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ServiceTaskController> _logger;

        public ServiceTaskController(ApplicationDbContext context, ILogger<ServiceTaskController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: ServiceTasks
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Rozpoczęcie pobierania listy wszystkich zadań serwisowych");

            try
            {
                var serviceTasks = await _context.ServiceTasks
                    .Include(st => st.ServiceOrder)
                    .ToListAsync();

                _logger.LogInformation("Pobrano {Count} zadań serwisowych", serviceTasks.Count);
                return View(serviceTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania listy zadań serwisowych");
                throw;
            }
        }

        // GET: ServiceTasks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            _logger.LogInformation("Rozpoczęcie pobierania szczegółów zadania o ID: {TaskId}", id);

            if (id == null)
            {
                _logger.LogWarning("Nie podano ID zadania serwisowego w szczegółach");
                return NotFound();
            }

            try
            {
                var serviceTask = await _context.ServiceTasks
                    .Include(st => st.ServiceOrder)
                    .Include(st => st.UsedParts)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (serviceTask == null)
                {
                    _logger.LogWarning("Nie znaleziono zadania serwisowego o ID: {TaskId}", id);
                    return NotFound();
                }

                _logger.LogInformation("Pobrano szczegóły zadania ID: {TaskId}, Opis: '{Description}', Koszt: {LaborCost:C}",
                    serviceTask.Id, serviceTask.Description, serviceTask.LaborCost);
                return View(serviceTask);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania szczegółów zadania o ID: {TaskId}", id);
                throw;
            }
        }

        // GET: ServiceTasks/Create
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Wyświetlanie formularza tworzenia nowego zadania serwisowego");

            try
            {
                var allOrders = await _context.ServiceOrders.CountAsync();
                _logger.LogInformation("Łączna liczba zleceń w bazie: {Count}", allOrders);

                if (allOrders == 0)
                {
                    _logger.LogWarning("Brak zleceń serwisowych w bazie danych");
                    ViewBag.ServiceOrders = new SelectList(new List<SelectListItem>(), "Value", "Text");
                    ViewBag.NoOrders = true;
                    return View();
                }

                var serviceOrders = await _context.ServiceOrders
                    .OrderBy(o => o.Id)
                    .ToListAsync();

                ViewBag.ServiceOrders = new SelectList(serviceOrders, "Id", "Id");

                _logger.LogDebug("Załadowano {Count} dostępnych zleceń dla formularza", serviceOrders.Count);
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas ładowania danych dla formularza zadania serwisowego");
                throw;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Description,LaborCost,ServiceOrderId")] ServiceTask serviceTask)
        {
            _logger.LogInformation("=== ROZPOCZĘCIE POST Create (FIXED) ===");
            _logger.LogInformation("Otrzymane dane - Description: '{Description}', LaborCost: {LaborCost}, ServiceOrderId: {ServiceOrderId}",
                serviceTask?.Description ?? "NULL", serviceTask?.LaborCost ?? 0, serviceTask?.ServiceOrderId ?? 0);

            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ModelState nie jest prawidłwy:");
                    foreach (var key in ModelState.Keys)
                    {
                        var state = ModelState[key];
                        if (state.Errors.Count > 0)
                        {
                            _logger.LogWarning("Klucz: {Key}, Błędy: {Errors}", key, string.Join("; ", state.Errors.Select(e => e.ErrorMessage)));
                        }
                    }
                }

                if (serviceTask == null)
                {
                    _logger.LogError("serviceTask jest null!");
                    ModelState.AddModelError("", "Dane zadania są puste");
                    await LoadFormData();
                    return View(serviceTask);
                }

                ModelState.Clear();

                if (string.IsNullOrWhiteSpace(serviceTask.Description))
                {
                    ModelState.AddModelError(nameof(serviceTask.Description), "Opis zadania jest wymagany");
                }
                else if (serviceTask.Description.Trim().Length < 3)
                {
                    ModelState.AddModelError(nameof(serviceTask.Description), "Opis musi mieć co najmniej 3 znaki");
                }

                if (serviceTask.LaborCost <= 0)
                {
                    ModelState.AddModelError(nameof(serviceTask.LaborCost), "Koszt robocizny musi być większy od 0");
                }

                if (serviceTask.ServiceOrderId <= 0)
                {
                    ModelState.AddModelError(nameof(serviceTask.ServiceOrderId), "Musisz wybrać zlecenie serwisowe");
                }
                else
                {
                    var serviceOrderExists = await _context.ServiceOrders
                        .AnyAsync(o => o.Id == serviceTask.ServiceOrderId);

                    if (!serviceOrderExists)
                    {
                        ModelState.AddModelError(nameof(serviceTask.ServiceOrderId), $"Zlecenie o ID {serviceTask.ServiceOrderId} nie istnieje");
                    }
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Błędy walidacji:");
                    foreach (var error in ModelState)
                    {
                        if (error.Value.Errors.Count > 0)
                        {
                            _logger.LogWarning("Pole {Field}: {Errors}", error.Key, string.Join("; ", error.Value.Errors.Select(e => e.ErrorMessage)));
                        }
                    }

                    await LoadFormData();
                    return View(serviceTask);
                }

                _logger.LogInformation("Zapisywanie zadania do bazy danych...");

                serviceTask.Description = serviceTask.Description.Trim();

                _context.ServiceTasks.Add(serviceTask);
                var result = await _context.SaveChangesAsync();

                _logger.LogInformation("SaveChanges zwrócił: {Result}", result);
                _logger.LogInformation("Pomyślnie utworzono zadanie serwisowe ID: {TaskId}, Opis: '{Description}', Koszt: {Cost}, Zlecenie: {OrderId}",
                    serviceTask.Id, serviceTask.Description, serviceTask.LaborCost, serviceTask.ServiceOrderId);

                TempData["SuccessMessage"] = $"Zadanie '{serviceTask.Description}' zostało pomyślnie dodane do zlecenia #{serviceTask.ServiceOrderId} (ID zadania: {serviceTask.Id})";

                _logger.LogInformation("Przekierowywanie do Index...");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas tworzenia zadania serwisowego");
                ModelState.AddModelError("", $"Wystąpił błąd podczas zapisywania: {ex.Message}");

                await LoadFormData();
                return View(serviceTask);
            }
        }

        [HttpGet]
        [Authorize(Policy = "WorkshopUser")]
        public async Task<IActionResult> GetTasksForEdit(int orderId)
        {
            _logger.LogInformation("Pobieranie zadań dla edycji zlecenia {OrderId} przez użytkownika: {UserName}",
                orderId, User.Identity?.Name);

            try
            {
                var orderQuery = _context.ServiceOrders.Where(o => o.Id == orderId);

                if (User.IsInRole("Mechanik"))
                {
                    var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    orderQuery = orderQuery.Where(o => o.AssignedMechanicId == currentUserId);
                }

                var orderExists = await orderQuery.AnyAsync();
                if (!orderExists)
                {
                    _logger.LogWarning("Zlecenie {OrderId} nie istnieje lub użytkownik {UserName} nie ma do niego dostępu",
                        orderId, User.Identity?.Name);
                    return Json(new { currentTasks = new List<object>(), availableTasks = new List<object>() });
                }

                var currentTasks = await _context.ServiceTasks
                    .Where(t => t.ServiceOrderId == orderId)
                    .Select(t => new {
                        id = t.Id,
                        description = t.Description,
                        laborCost = t.LaborCost.ToString("F2"),
                        serviceOrderId = t.ServiceOrderId
                    })
                    .OrderBy(t => t.description)
                    .ToListAsync();

                var availableTasks = await _context.ServiceTasks
                    .Where(t => t.ServiceOrderId == null || t.ServiceOrderId == 0)
                    .Select(t => new {
                        id = t.Id,
                        description = t.Description,
                        laborCost = t.LaborCost.ToString("F2"),
                        serviceOrderId = t.ServiceOrderId
                    })
                    .OrderBy(t => t.description)
                    .ToListAsync();

                var result = new
                {
                    currentTasks = currentTasks,
                    availableTasks = availableTasks
                };

                _logger.LogInformation("Zwrócono {CurrentCount} obecnych i {AvailableCount} dostępnych zadań dla zlecenia {OrderId}",
                    currentTasks.Count, availableTasks.Count, orderId);

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania zadań dla edycji zlecenia {OrderId}", orderId);
                return Json(new { currentTasks = new List<object>(), availableTasks = new List<object>() });
            }
        }

        private async Task LoadFormData()
        {
            var serviceOrders = await _context.ServiceOrders
                .OrderBy(o => o.Id)
                .ToListAsync();

            ViewBag.ServiceOrders = new SelectList(serviceOrders, "Id", "Id");
        }

        // GET: ServiceTasks/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            _logger.LogInformation("Wyświetlanie formularza edycji zadania o ID: {TaskId}", id);

            if (id == null)
            {
                _logger.LogWarning("Nie podano ID zadania serwisowego do edycji");
                return NotFound();
            }

            try
            {
                var serviceTask = await _context.ServiceTasks.FindAsync(id);
                if (serviceTask == null)
                {
                    _logger.LogWarning("Nie znaleziono zadania serwisowego o ID: {TaskId} do edycji", id);
                    return NotFound();
                }

                await LoadServiceOrdersForDropdown();

                _logger.LogInformation("Załadowano zadanie do edycji: ID: {TaskId}, Opis: '{Description}'",
                    serviceTask.Id, serviceTask.Description);
                return View(serviceTask);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas ładowania zadania o ID: {TaskId} do edycji", id);
                throw;
            }
        }

        // POST: ServiceTasks/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Description,LaborCost,ServiceOrderId")] ServiceTask serviceTask)
        {
            _logger.LogInformation("Rozpoczęcie aktualizacji zadania o ID: {TaskId}", id);

            if (id != serviceTask.Id)
            {
                _logger.LogWarning("Niezgodność ID zadania: {ExpectedId} vs {ActualId}", id, serviceTask.Id);
                return NotFound();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    var orderExists = await _context.ServiceOrders.AnyAsync(o => o.Id == serviceTask.ServiceOrderId);
                    if (!orderExists)
                    {
                        ModelState.AddModelError("ServiceOrderId", "Wybrane zlecenie serwisowe nie istnieje.");
                        await LoadServiceOrdersForDropdown();
                        return View(serviceTask);
                    }

                    try
                    {
                        _context.Update(serviceTask);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation("Pomyślnie zaktualizowano zadanie o ID: {TaskId}", id);
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        _logger.LogError(ex, "Błąd współbieżności podczas aktualizacji zadania o ID: {TaskId}", id);

                        if (!ServiceTaskExists(serviceTask.Id))
                        {
                            _logger.LogWarning("Zadanie o ID: {TaskId} nie istnieje", serviceTask.Id);
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogWarning("Model zadania nie jest prawidłowy podczas edycji ID: {TaskId}", id);
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning("Błąd walidacji: {ErrorMessage}", error.ErrorMessage);
                }

                await LoadServiceOrdersForDropdown();
                return View(serviceTask);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas aktualizacji zadania o ID: {TaskId}", id);
                await LoadServiceOrdersForDropdown();
                ModelState.AddModelError("", "Wystąpił błąd podczas aktualizacji zadania.");
                return View(serviceTask);
            }
        }

        // GET: ServiceTasks/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            _logger.LogInformation("Wyświetlanie potwierdzenia usunięcia zadania o ID: {TaskId}", id);

            if (id == null)
            {
                _logger.LogWarning("Nie podano ID zadania serwisowego do usunięcia");
                return NotFound();
            }

            try
            {
                var serviceTask = await _context.ServiceTasks
                    .Include(st => st.ServiceOrder)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (serviceTask == null)
                {
                    _logger.LogWarning("Nie znaleziono zadania serwisowego o ID: {TaskId} do usunięcia", id);
                    return NotFound();
                }

                _logger.LogInformation("Załadowano zadanie do potwierdzenia usunięcia: ID: {TaskId}, Opis: '{Description}'",
                    serviceTask.Id, serviceTask.Description);
                return View(serviceTask);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas ładowania zadania o ID: {TaskId} do usunięcia", id);
                throw;
            }
        }

        // POST: ServiceTasks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            _logger.LogInformation("Rozpoczęcie usuwania zadania o ID: {TaskId}", id);

            try
            {
                var serviceTask = await _context.ServiceTasks.FindAsync(id);
                if (serviceTask != null)
                {
                    _logger.LogInformation("Usuwanie zadania: ID: {TaskId}, Opis: '{Description}'",
                        serviceTask.Id, serviceTask.Description);

                    _context.ServiceTasks.Remove(serviceTask);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Pomyślnie usunięto zadanie o ID: {TaskId}", id);
                }
                else
                {
                    _logger.LogWarning("Nie znaleziono zadania o ID: {TaskId} do usunięcia", id);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas usuwania zadania o ID: {TaskId}", id);
                TempData["Error"] = "Wystąpił błąd podczas usuwania zadania.";
                return RedirectToAction(nameof(Index));
            }
        }

        private bool ServiceTaskExists(int id)
        {
            try
            {
                var exists = _context.ServiceTasks.Any(e => e.Id == id);
                _logger.LogDebug("Sprawdzenie istnienia zadania o ID: {TaskId} - rezultat: {Exists}", id, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas sprawdzania istnienia zadania o ID: {TaskId}", id);
                return false;
            }
        }

        private async Task LoadServiceOrdersForDropdown()
        {
            try
            {
                var serviceOrders = await _context.ServiceOrders
                    .Where(o => o.Status != "Zakończone")
                    .Select(o => new {
                        o.Id,
                        DisplayText = $"Zlecenie #{o.Id} ({o.Status})"
                    })
                    .ToListAsync();

                ViewBag.ServiceOrders = new SelectList(serviceOrders, "Id", "DisplayText");

                _logger.LogDebug("Załadowano {Count} zleceń serwisowych do dropdown", serviceOrders.Count);

                if (serviceOrders.Count == 0)
                {
                    _logger.LogWarning("Brak dostępnych zleceń serwisowych - sprawdź bazę danych");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas ładowania zleceń serwisowych dla dropdown");
                ViewBag.ServiceOrders = new SelectList(new List<object>(), "Id", "DisplayText");
            }
        }

        [HttpGet]
        [Authorize(Policy = "WorkshopUser")]
        public async Task<IActionResult> GetAvailableTasks()
        {
            _logger.LogInformation("Pobieranie dostępnych zadań serwisowych dla AJAX przez użytkownika: {UserName}",
                User.Identity?.Name);

            try
            {
                var tasks = await _context.ServiceTasks
                    .Where(t => t.ServiceOrderId == null || t.ServiceOrderId == 0)
                    .Select(t => new {
                        id = t.Id,
                        description = t.Description,
                        laborCost = t.LaborCost.ToString("F2"),
                        serviceOrderId = t.ServiceOrderId
                    })
                    .OrderBy(t => t.description)
                    .ToListAsync();

                _logger.LogInformation("Zwrócono {Count} zadań serwisowych dla użytkownika {UserName}",
                    tasks.Count, User.Identity?.Name);

                return Json(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania zadań serwisowych dla AJAX");
                return Json(new List<object>());
            }
        }
    }
}