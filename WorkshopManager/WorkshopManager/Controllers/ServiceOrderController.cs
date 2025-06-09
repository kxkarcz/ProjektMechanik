using Microsoft.AspNetCore.Mvc;
using WorkshopManager.DTOs;
using WorkshopManager.Data;
using WorkshopManager.Models;
using Microsoft.EntityFrameworkCore;
using WorkshopManager.Services;
using WorkshopManager.ViewModels;
using Microsoft.AspNetCore.Authorization;
using WorkshopManager.Extensions;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace WorkshopManager.Controllers
{
    [Authorize(Policy = "WorkshopUser")] // Wszyscy zalogowani użytkownicy mogą przeglądać zlecenia
    public class ServiceOrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ServiceOrderMapper _mapper;
        private readonly ILogger<ServiceOrderController> _logger;
        private readonly ICommentService _commentService;

        public ServiceOrderController(ApplicationDbContext context, ServiceOrderMapper mapper, ILogger<ServiceOrderController> logger, ICommentService commentService)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _commentService = commentService;
        }

        // GET: ServiceOrders - Wszyscy mogą przeglądać
        public IActionResult Index()
        {
            _logger.LogInformation("Rozpoczęcie pobierania listy wszystkich zleceń serwisowych dla użytkownika: {UserName}",
                User.Identity?.Name);

            try
            {
                var ordersQuery = _context.ServiceOrders
                    .Include(o => o.ServiceTasks)
                    .Include(o => o.Comments)
                    .AsQueryable();

                if (User.IsInRole("Mechanik"))
                {
                    var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    ordersQuery = ordersQuery.Where(o => o.AssignedMechanicId == currentUserId);
                    _logger.LogInformation("Filtrowanie zleceń dla mechanika: {UserId}", currentUserId);
                }

                var orders = ordersQuery.Select(o => new ServiceOrderDto
                {
                    Id = o.Id,
                    Status = o.Status,
                    VehicleId = o.VehicleId,
                    AssignedMechanicId = o.AssignedMechanicId,
                    ServiceTaskIds = o.ServiceTasks.Select(t => t.Id).ToList(),
                    CommentIds = o.Comments.Select(c => c.Id).ToList()
                }).ToList();

                var statusCounts = orders.GroupBy(o => o.Status)
                    .ToDictionary(g => g.Key, g => g.Count());

                var totalTasks = orders.Sum(o => o.ServiceTaskIds.Count);
                var totalComments = orders.Sum(o => o.CommentIds.Count);

                _logger.LogInformation("Pobrano {OrderCount} zleceń serwisowych z {TaskCount} zadaniami i {CommentCount} komentarzami dla użytkownika {UserName}. Statusy: {StatusBreakdown}",
                    orders.Count, totalTasks, totalComments, User.Identity?.Name,
                    string.Join(", ", statusCounts.Select(kv => $"{kv.Key}: {kv.Value}")));

                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania listy zleceń serwisowych dla użytkownika: {UserName}",
                    User.Identity?.Name);
                TempData["ErrorMessage"] = "Wystąpił błąd podczas pobierania zleceń serwisowych.";
                return View(new List<ServiceOrderDto>());
            }
        }

        // GET: ServiceOrders/Details/5 - Wszyscy mogą przeglądać szczegóły
        public async Task<IActionResult> Details(int? id)
        {
            _logger.LogInformation("Rozpoczęcie pobierania szczegółów zlecenia o ID: {OrderId} przez użytkownika: {UserName}",
                id, User.Identity?.Name);

            if (id == null)
            {
                _logger.LogWarning("Nie podano ID zlecenia w szczegółach");
                return NotFound();
            }

            try
            {
                var orderQuery = _context.ServiceOrders
                    .Include(o => o.ServiceTasks)
                    .Include(o => o.Comments.OrderByDescending(c => c.Timestamp))
                    .Include(o => o.UsedParts) 
                        .ThenInclude(up => up.Part) 
                    .Where(o => o.Id == id);

                if (User.IsInRole("Mechanik"))
                {
                    var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    orderQuery = orderQuery.Where(o => o.AssignedMechanicId == currentUserId);
                }

                var order = orderQuery.FirstOrDefault();

                if (order == null)
                {
                    _logger.LogWarning("Nie znaleziono zlecenia o ID: {OrderId} dla użytkownika: {UserName}",
                        id, User.Identity?.Name);
                    return NotFound();
                }

                var comments = await _commentService.GetCommentsByOrderIdAsync(id.Value);

                var orderDetailsViewModel = new ServiceOrderDetailsViewModel
                {
                    Order = new ServiceOrderDto
                    {
                        Id = order.Id,
                        Status = order.Status,
                        VehicleId = order.VehicleId,
                        AssignedMechanicId = order.AssignedMechanicId,
                        ServiceTaskIds = order.ServiceTasks.Select(t => t.Id).ToList(),
                        CommentIds = order.Comments.Select(c => c.Id).ToList()
                    },
                    Comments = comments,
                    NewComment = new CommentCreatedDto { ServiceOrderId = id.Value },
                    UsedParts = order.UsedParts.ToList() 
                };

                _logger.LogInformation("Pobrano szczegóły zlecenia ID: {OrderId}, Status: {Status}, Pojazd: {VehicleId}, Mechanik: {MechanicId}, Zadania: {TaskCount}, Komentarze: {CommentCount}, Części: {PartsCount}",
                    order.Id, order.Status, order.VehicleId, order.AssignedMechanicId,
                    order.ServiceTasks.Count, comments.Count, order.UsedParts.Count);

                return View(orderDetailsViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania szczegółów zlecenia o ID: {OrderId}", id);
                return NotFound();
            }
        }

        // POST: ServiceOrders/AddComment - Wszyscy mogą dodawać komentarze
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int orderId, [Bind(Prefix = "NewComment")] CommentCreatedDto commentDto)
        {
            _logger.LogInformation("Rozpoczęcie dodawania komentarza do zlecenia ID: {OrderId} przez użytkownika: {UserName}",
                orderId, User.Identity?.Name);

            var orderExists = await _context.ServiceOrders.AnyAsync(o => o.Id == orderId);
            if (!orderExists)
            {
                _logger.LogWarning("Próba dodania komentarza do nieistniejącego zlecenia ID: {OrderId}", orderId);
                TempData["ErrorMessage"] = "Zlecenie nie istnieje.";
                return RedirectToAction(nameof(Index));
            }

            if (User.IsInRole("Mechanik"))
            {
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var order = await _context.ServiceOrders.FirstOrDefaultAsync(o => o.Id == orderId);
                if (order?.AssignedMechanicId != currentUserId)
                {
                    _logger.LogWarning("Mechanik {UserId} próbuje dodać komentarz do zlecenia {OrderId}, które nie jest do niego przypisane",
                        currentUserId, orderId);
                    TempData["ErrorMessage"] = "Nie masz uprawnień do dodawania komentarzy do tego zlecenia.";
                    return RedirectToAction(nameof(Index));
                }
            }

            if (commentDto != null)
            {
                commentDto.ServiceOrderId = orderId;
                commentDto.Author = User.Identity?.Name ?? "Nieznany użytkownik";
            }

            if (commentDto == null)
            {
                _logger.LogWarning("Otrzymano pusty obiekt komentarza dla zlecenia ID: {OrderId}", orderId);
                TempData["ErrorMessage"] = "Dane komentarza są nieprawidłowe.";
                return RedirectToAction(nameof(Details), new { id = orderId });
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model komentarza nie jest prawidłowy dla zlecenia ID: {OrderId}. Błędy: {Errors}",
                    orderId, string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

                var order = await _context.ServiceOrders
                    .Include(o => o.ServiceTasks)
                    .Include(o => o.Comments.OrderByDescending(c => c.Timestamp))
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    return NotFound();
                }

                var comments = await _commentService.GetCommentsByOrderIdAsync(orderId);

                var orderDetailsViewModel = new ServiceOrderDetailsViewModel
                {
                    Order = new ServiceOrderDto
                    {
                        Id = order.Id,
                        Status = order.Status,
                        VehicleId = order.VehicleId,
                        AssignedMechanicId = order.AssignedMechanicId,
                        ServiceTaskIds = order.ServiceTasks.Select(t => t.Id).ToList(),
                        CommentIds = order.Comments.Select(c => c.Id).ToList()
                    },
                    Comments = comments,
                    NewComment = commentDto
                };

                return View("Details", orderDetailsViewModel);
            }

            try
            {
                await _commentService.CreateCommentAsync(commentDto);

                _logger.LogInformation("Pomyślnie dodano komentarz do zlecenia ID: {OrderId} przez użytkownika: {UserName}",
                    orderId, User.Identity?.Name);

                TempData["SuccessMessage"] = "Komentarz został pomyślnie dodany.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas dodawania komentarza do zlecenia ID: {OrderId}", orderId);
                TempData["ErrorMessage"] = "Wystąpił błąd podczas dodawania komentarza: " + ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id = orderId });
        }
        [HttpGet]
        [Authorize(Policy = "WorkshopUser")]
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Wyświetlanie formularza tworzenia nowego zlecenia dla użytkownika: {UserName}", User.Identity?.Name);

            try
            {
                await LoadFormDataAsync();
                return View(new ServiceOrderCreateDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas ładowania formularza tworzenia zlecenia");
                TempData["ErrorMessage"] = "Wystąpił błąd podczas ładowania formularza.";
                return View(new ServiceOrderCreateDto());
            }
        }



        // POST: ServiceOrders/DeleteComment - Tylko Admin i Recepcjonista mogą usuwać komentarze
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOrRecepcjonista")]
        public async Task<IActionResult> DeleteComment(int commentId, int orderId)
        {
            _logger.LogInformation("Rozpoczęcie usuwania komentarza ID: {CommentId} ze zlecenia ID: {OrderId} przez użytkownika: {UserName}",
                commentId, orderId, User.Identity?.Name);

            try
            {
                var result = await _commentService.DeleteCommentAsync(commentId);
                if (result)
                {
                    _logger.LogInformation("Pomyślnie usunięto komentarz ID: {CommentId} ze zlecenia ID: {OrderId}",
                        commentId, orderId);
                    TempData["SuccessMessage"] = "Komentarz został usunięty.";
                }
                else
                {
                    _logger.LogWarning("Nie znaleziono komentarza ID: {CommentId} do usunięcia", commentId);
                    TempData["WarningMessage"] = "Komentarz nie został znaleziony.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas usuwania komentarza ID: {CommentId}", commentId);
                TempData["ErrorMessage"] = "Wystąpił błąd podczas usuwania komentarza.";
            }

            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        [HttpGet]
        [Authorize(Policy = "WorkshopUser")]
        public async Task<IActionResult> Edit(int? id)
        {
            _logger.LogInformation("Wyświetlanie formularza edycji zlecenia o ID: {OrderId} przez użytkownika: {UserName}",
                id, User.Identity?.Name);

            if (id == null)
            {
                _logger.LogWarning("Nie podano ID zlecenia do edycji");
                return NotFound();
            }

            try
            {
                var orderQuery = _context.ServiceOrders
                    .Include(o => o.ServiceTasks)
                    .Where(o => o.Id == id);

                if (User.IsInRole("Mechanik"))
                {
                    var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    orderQuery = orderQuery.Where(o => o.AssignedMechanicId == currentUserId);
                }

                var order = await orderQuery.FirstOrDefaultAsync();

                if (order == null)
                {
                    _logger.LogWarning("Nie znaleziono zlecenia o ID: {OrderId} do edycji dla użytkownika: {UserName}",
                        id, User.Identity?.Name);
                    return NotFound();
                }

                var updateDto = new ServiceOrderUpdateDto
                {
                    Id = order.Id,
                    Status = order.Status,
                    VehicleId = order.VehicleId,
                    AssignedMechanicId = order.AssignedMechanicId,
                    ServiceTaskIds = order.ServiceTasks.Select(t => t.Id).ToList()
                };

                await LoadEditFormDataAsync(order);

                _logger.LogInformation("Załadowano zlecenie do edycji: ID: {OrderId}, Status: {Status}, Pojazd: {VehicleId}, Mechanik: {MechanicId}, Zadania: {TaskCount}",
                    updateDto.Id, updateDto.Status, updateDto.VehicleId, updateDto.AssignedMechanicId, updateDto.ServiceTaskIds.Count);

                ViewBag.Id = id;
                return View(updateDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas ładowania zlecenia {OrderId} do edycji", id);
                TempData["ErrorMessage"] = "Wystąpił błąd podczas ładowania zlecenia.";
                return RedirectToAction(nameof(Index));
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "WorkshopUser")]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Status,VehicleId,AssignedMechanicId")] ServiceOrderUpdateDto orderDto,
            string taskIdsCsv = "",
            string newTasksJson = "[]",
            string tasksToRemove = "",
            string partsJson = "[]") 
        {
            try
            {
                if (string.IsNullOrEmpty(newTasksJson))
                    newTasksJson = "[]";

                if (string.IsNullOrEmpty(taskIdsCsv))
                    taskIdsCsv = "";

                if (string.IsNullOrEmpty(tasksToRemove))
                    tasksToRemove = "";

                if (string.IsNullOrEmpty(partsJson)) 
                    partsJson = "[]";

                _logger.LogInformation("Edycja zlecenia {OrderId}: taskIdsCsv='{TaskIds}', newTasksJson='{NewTasks}', tasksToRemove='{RemoveTasks}', partsJson='{Parts}'",
                    id, taskIdsCsv, newTasksJson, tasksToRemove, partsJson);

                var orderQuery = _context.ServiceOrders
                    .Include(o => o.ServiceTasks)
                    .Where(o => o.Id == id);

                if (User.IsInRole("Mechanik"))
                {
                    var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    orderQuery = orderQuery.Where(o => o.AssignedMechanicId == currentUserId);
                }

                var order = await orderQuery.FirstOrDefaultAsync();

                if (order == null)
                {
                    _logger.LogWarning("Nie znaleziono zlecenia o ID: {OrderId} do edycji dla użytkownika: {UserName}",
                        id, User.Identity?.Name);
                    return NotFound();
                }

                order.Status = orderDto.Status;
                order.VehicleId = orderDto.VehicleId;

                if (User.IsInRole("Admin") || User.IsInRole("Recepcjonista"))
                {
                    order.AssignedMechanicId = orderDto.AssignedMechanicId;
                }

                await ProcessTaskUpdates(id, taskIdsCsv, newTasksJson, tasksToRemove);

                var partsAdded = 0;
                if (!string.IsNullOrEmpty(partsJson) && partsJson != "[]")
                {
                    var parts = JsonConvert.DeserializeObject<List<PartSelectionDto>>(partsJson);
                    if (parts?.Any() == true)
                    {
                        partsAdded = await ProcessPartsForOrder(id, parts);
                    }
                }

                _context.Update(order);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Pomyślnie zaktualizowano zlecenie o ID: {OrderId} z {PartsCount} nowymi częściami", id, partsAdded);
                TempData["SuccessMessage"] = $"Zlecenie #{id} zostało pomyślnie zaktualizowane" + (partsAdded > 0 ? $" z {partsAdded} nowymi częściami." : ".");

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas edycji zlecenia {OrderId}", id);
                TempData["ErrorMessage"] = "Wystąpił błąd podczas zapisywania zmian: " + ex.Message;

                var order = await _context.ServiceOrders.FindAsync(id);
                if (order != null)
                {
                    await LoadEditFormDataAsync(order);
                    ViewBag.Id = id;

                    var updateDto = new ServiceOrderUpdateDto
                    {
                        Id = order.Id,
                        Status = order.Status,
                        VehicleId = order.VehicleId,
                        AssignedMechanicId = order.AssignedMechanicId
                    };

                    return View(updateDto);
                }

                return RedirectToAction(nameof(Index));
            }
        }

        private async Task ProcessTaskUpdates(int orderId, string taskIdsCsv, string newTasksJson, string tasksToRemove)
        {
            if (!string.IsNullOrEmpty(tasksToRemove))
            {
                var taskIdsToRemove = tasksToRemove.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)
                    .ToList();

                if (taskIdsToRemove.Any())
                {
                    var tasksToUnassign = await _context.ServiceTasks
                        .Where(t => taskIdsToRemove.Contains(t.Id) && t.ServiceOrderId == orderId)
                        .ToListAsync();

                    foreach (var task in tasksToUnassign)
                    {
                        task.ServiceOrderId = null; 
                    }

                    _context.ServiceTasks.UpdateRange(tasksToUnassign);
                }
            }

            if (!string.IsNullOrEmpty(taskIdsCsv))
            {
                var taskIds = taskIdsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)
                    .Where(id => id < 10000) 
                    .ToList();

                if (taskIds.Any())
                {
                    var tasksToAdd = await _context.ServiceTasks
                        .Where(t => taskIds.Contains(t.Id) && t.ServiceOrderId == null)
                        .ToListAsync();

                    foreach (var task in tasksToAdd)
                    {
                        task.ServiceOrderId = orderId;
                    }

                    _context.ServiceTasks.UpdateRange(tasksToAdd);
                }
            }

            if (!string.IsNullOrEmpty(newTasksJson) && newTasksJson != "[]")
            {
                var newTasks = JsonConvert.DeserializeObject<List<NewTaskDto>>(newTasksJson);
                if (newTasks?.Any() == true)
                {
                    foreach (var taskDto in newTasks)
                    {
                        _context.ServiceTasks.Add(new ServiceTask
                        {
                            Description = taskDto.Description?.Trim() ?? string.Empty,
                            LaborCost = taskDto.LaborCost,
                            ServiceOrderId = orderId
                        });
                    }
                }
            }
        }

        private async Task LoadEditFormDataAsync(ServiceOrder order)
        {
            try
            {
                var vehiclesFromDb = await _context.Vehicles
                    .Include(v => v.Customer)
                    .ToListAsync(); 

                var vehicles = vehiclesFromDb.Select(v => new {
                    v.Id,
                    DisplayText = $"{v.RegistrationNumber} - {v.Customer.FirstName} {v.Customer.LastName} (VIN: {v.VIN.Substring(0, Math.Min(v.VIN.Length, 8))}...)"
                }).OrderBy(v => v.DisplayText).ToList();

                ViewBag.Vehicles = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(vehicles, "Id", "DisplayText", order.VehicleId);

                bool canChangeAssignment = User.IsInRole("Admin") || User.IsInRole("Recepcjonista");
                ViewBag.CanChangeAssignment = canChangeAssignment;

                if (canChangeAssignment)
                {
                    var userManager = HttpContext.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
                    var mechanicUsers = await userManager.GetUsersInRoleAsync("Mechanik");

                    var mechanics = mechanicUsers.Select(u => new {
                        Id = u.Id,
                        DisplayText = $"{u.Email} ({u.UserName})"
                    }).OrderBy(m => m.DisplayText).ToList();

                    ViewBag.Mechanics = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(mechanics, "Id", "DisplayText", order.AssignedMechanicId);
                }
                else if (User.IsInRole("Mechanik"))
                {
                    var userManager = HttpContext.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
                    if (!string.IsNullOrEmpty(order.AssignedMechanicId))
                    {
                        var mechanic = await userManager.FindByIdAsync(order.AssignedMechanicId);
                        ViewBag.CurrentMechanicName = mechanic?.Email ?? "Nieznany mechanik";
                    }
                    else
                    {
                        ViewBag.CurrentMechanicName = "Nieprzypisany";
                    }
                }

                _logger.LogDebug("Załadowano dane formularza edycji dla zlecenia {OrderId}", order.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas ładowania danych formularza edycji");

                ViewBag.Vehicles = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(new List<object>(), "Id", "DisplayText");
                ViewBag.Mechanics = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(new List<object>(), "Id", "DisplayText");
                ViewBag.CanChangeAssignment = false;

                throw;
            }
        }


        [HttpGet]
        [Authorize(Policy = "AdminOrRecepcjonista")]
        public async Task<IActionResult> Delete(int? id)
        {
            _logger.LogInformation("Wyświetlanie potwierdzenia usunięcia zlecenia o ID: {OrderId} przez użytkownika: {UserName}",
                id, User.Identity?.Name);

            if (id == null)
            {
                _logger.LogWarning("Nie podano ID zlecenia do usunięcia");
                return NotFound();
            }

            try
            {
                var order = await _context.ServiceOrders
                    .Include(o => o.ServiceTasks)
                    .Include(o => o.Comments)
                    .Include(o => o.Vehicle)
                        .ThenInclude(v => v.Customer)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (order == null)
                {
                    _logger.LogWarning("Nie znaleziono zlecenia o ID: {OrderId} do usunięcia", id);
                    return NotFound();
                }

                string mechanicName = string.Empty;
                if (!string.IsNullOrEmpty(order.AssignedMechanicId))
                {
                    var userManager = HttpContext.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
                    var mechanic = await userManager.FindByIdAsync(order.AssignedMechanicId);
                    mechanicName = mechanic?.Email ?? "Nieznany mechanik";
                }

                var orderDto = new ServiceOrderDto
                {
                    Id = order.Id,
                    Status = order.Status,
                    VehicleId = order.VehicleId,
                    AssignedMechanicId = order.AssignedMechanicId,
                    ServiceTaskIds = order.ServiceTasks.Select(t => t.Id).ToList(),
                    CommentIds = order.Comments.Select(c => c.Id).ToList(),
                    CreatedAt = order.CreatedAt,
                    VehicleLicensePlate = order.Vehicle?.RegistrationNumber ?? "Brak numeru",
                    AssignedMechanicName = mechanicName
                };

                _logger.LogInformation("Załadowano zlecenie do potwierdzenia usunięcia: ID: {OrderId}, Status: {Status}, Pojazd: {VehicleId}, Zadania: {TaskCount}, Komentarze: {CommentCount}",
                    orderDto.Id, orderDto.Status, orderDto.VehicleId, orderDto.ServiceTaskIds.Count, orderDto.CommentIds.Count);

                return View(orderDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas ładowania zlecenia o ID: {OrderId} do usunięcia", id);
                TempData["ErrorMessage"] = "Wystąpił błąd podczas ładowania zlecenia.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOrRecepcjonista")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            _logger.LogInformation("Rozpoczęcie usuwania zlecenia o ID: {OrderId} przez użytkownika: {UserName}",
                id, User.Identity?.Name);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.ServiceOrders
                    .Include(o => o.ServiceTasks)
                    .Include(o => o.Comments)
                    .Include(o => o.UsedParts)
                        .ThenInclude(up => up.Part)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order != null)
                {
                    _logger.LogInformation("Usuwanie zlecenia: ID: {OrderId}, Status: {Status}, Pojazd: {VehicleId}, Mechanik: {MechanicId}",
                        order.Id, order.Status, order.VehicleId, order.AssignedMechanicId);

                    foreach (var usedPart in order.UsedParts)
                    {
                        if (usedPart.Part != null)
                        {
                            usedPart.Part.StockQuantity += usedPart.Quantity;
                            _context.Update(usedPart.Part);
                        }
                    }

                    _context.UsedParts.RemoveRange(order.UsedParts);

                    foreach (var task in order.ServiceTasks)
                    {
                        task.ServiceOrderId = null;
                    }
                    _context.ServiceTasks.UpdateRange(order.ServiceTasks);

                    _context.Comments.RemoveRange(order.Comments);

                    _context.ServiceOrders.Remove(order);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Pomyślnie usunięto zlecenie o ID: {OrderId}", id);
                    TempData["SuccessMessage"] = $"Zlecenie #{id} zostało pomyślnie usunięte.";
                }
                else
                {
                    _logger.LogWarning("Nie znaleziono zlecenia o ID: {OrderId} do usunięcia", id);
                    TempData["WarningMessage"] = "Zlecenie nie zostało znalezione.";
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Błąd podczas usuwania zlecenia o ID: {OrderId}", id);
                TempData["ErrorMessage"] = "Wystąpił błąd podczas usuwania zlecenia.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            try
            {
                var exists = _context.ServiceOrders.Any(e => e.Id == id);
                _logger.LogDebug("Sprawdzenie istnienia zlecenia o ID: {OrderId} - rezultat: {Exists}", id, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas sprawdzania istnienia zlecenia o ID: {OrderId}", id);
                return false;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "WorkshopUser")]
        public async Task<IActionResult> Create(
     [Bind("VehicleId,Status,AssignedMechanicId")] ServiceOrderCreateDto orderDto,
     string taskIdsCsv = "",
     string newTasksJson = "[]",
     string partsJson = "[]")
        {
            _logger.LogInformation("Rozpoczęcie tworzenia nowego zlecenia przez użytkownika: {UserName}", User.Identity?.Name);

            try
            {
                if (string.IsNullOrEmpty(newTasksJson))
                    newTasksJson = "[]";

                if (string.IsNullOrEmpty(partsJson))
                    partsJson = "[]";

                if (string.IsNullOrEmpty(taskIdsCsv))
                    taskIdsCsv = "";

                _logger.LogInformation("Otrzymane dane: taskIdsCsv='{TaskIds}', newTasksJson='{NewTasks}', partsJson='{Parts}'",
                    taskIdsCsv, newTasksJson, partsJson);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model zlecenia nie jest prawidłowy. Błędy: {Errors}",
                        string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

                    await LoadFormDataAsync();
                    return View(orderDto);
                }

                if (User.IsInRole("Mechanik") && string.IsNullOrEmpty(orderDto.AssignedMechanicId))
                {
                    orderDto.AssignedMechanicId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                }

                var order = new ServiceOrder
                {
                    VehicleId = orderDto.VehicleId,
                    Status = orderDto.Status,
                    AssignedMechanicId = orderDto.AssignedMechanicId
                };

                _context.ServiceOrders.Add(order);
                await _context.SaveChangesAsync();

                var orderId = order.Id;
                _logger.LogInformation("Utworzono zlecenie o ID: {OrderId}", orderId);

                var tasksAdded = await ProcessTasksForOrder(orderId, taskIdsCsv, newTasksJson);

                var partsAdded = 0;
                if (!string.IsNullOrEmpty(partsJson) && partsJson != "[]")
                {
                    var parts = JsonConvert.DeserializeObject<List<PartSelectionDto>>(partsJson);
                    if (parts?.Any() == true)
                    {
                        partsAdded = await ProcessPartsForOrder(orderId, parts);
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Pomyślnie utworzono zlecenie ID: {OrderId} z {TaskCount} zadaniami i {PartCount} częściami",
                    orderId, tasksAdded, partsAdded);

                TempData["SuccessMessage"] = $"Zlecenie #{orderId} zostało pomyślnie utworzone z {tasksAdded} zadaniami i {partsAdded} częściami.";
                return RedirectToAction(nameof(Details), new { id = orderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas tworzenia zlecenia");
                TempData["ErrorMessage"] = "Wystąpił błąd podczas tworzenia zlecenia: " + ex.Message;

                try
                {
                    await LoadFormDataAsync();
                }
                catch (Exception loadEx)
                {
                    _logger.LogError(loadEx, "Błąd podczas ponownego ładowania danych formularza");
                }

                return View(orderDto);
            }
        }


        private async Task LoadFormDataAsync()
        {
            try
            {
                var vehiclesFromDb = await _context.Vehicles
                    .Include(v => v.Customer)
                    .ToListAsync();

                var vehicles = vehiclesFromDb.Select(v => new
                {
                    v.Id,
                    DisplayText = $"{v.RegistrationNumber} - {v.Customer.FirstName} {v.Customer.LastName} (VIN: {v.VIN.Substring(0, Math.Min(v.VIN.Length, 8))}...)"
                }).OrderBy(v => v.DisplayText).ToList();

                ViewBag.Vehicles = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(vehicles, "Id", "DisplayText");

                bool canAssignMechanic = User.IsInRole("Admin") || User.IsInRole("Recepcjonista");
                ViewBag.CanAssignMechanic = canAssignMechanic;

                if (canAssignMechanic)
                {
                    var userManager = HttpContext.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
                    var mechanicUsers = await userManager.GetUsersInRoleAsync("Mechanik");

                    var mechanics = mechanicUsers.Select(u => new
                    {
                        Id = u.Id,
                        DisplayText = $"{u.Email} ({u.UserName})"
                    }).OrderBy(m => m.DisplayText).ToList();

                    ViewBag.Mechanics = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(mechanics, "Id", "DisplayText");
                }
                else if (User.IsInRole("Mechanik"))
                {
                    ViewBag.CurrentMechanicName = User.Identity?.Name ?? "Bieżący mechanik";
                }

                _logger.LogDebug("Załadowano {VehicleCount} pojazdów i {MechanicAccess} mechaników dla formularza",
                    vehicles.Count, canAssignMechanic ? "z dostępem do" : "bez dostępu do");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas ładowania danych formularza");

                ViewBag.Vehicles = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(new List<object>(), "Id", "DisplayText");
                ViewBag.Mechanics = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(new List<object>(), "Id", "DisplayText");
                ViewBag.CanAssignMechanic = false;

                throw;
            }
        }

        private async Task<int> ProcessTasksForOrder(int orderId, string taskIdsCsv, string newTasksJson)
        {
            var tasksAdded = 0;

            if (!string.IsNullOrEmpty(taskIdsCsv))
            {
                var taskIds = taskIdsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)
                    .Where(id => id < 10000)
                    .ToList();

                if (taskIds.Any())
                {
                    var tasksToAdd = await _context.ServiceTasks
                        .Where(t => taskIds.Contains(t.Id) && t.ServiceOrderId == null)
                        .ToListAsync();

                    foreach (var task in tasksToAdd)
                    {
                        task.ServiceOrderId = orderId;
                        tasksAdded++;
                    }
                    _context.ServiceTasks.UpdateRange(tasksToAdd);
                }
            }

            if (!string.IsNullOrEmpty(newTasksJson))
            {
                var newTasks = JsonConvert.DeserializeObject<List<NewTaskDto>>(newTasksJson);
                if (newTasks?.Any() == true)
                {
                    foreach (var taskDto in newTasks)
                    {
                        _context.ServiceTasks.Add(new ServiceTask
                        {
                            Description = taskDto.Description?.Trim() ?? string.Empty,
                            LaborCost = taskDto.LaborCost,
                            ServiceOrderId = orderId
                        });
                        tasksAdded++;
                    }
                }
            }

            return tasksAdded;
        }

        private async Task<int> ProcessPartsForOrder(int orderId, List<PartSelectionDto> parts)
        {
            var partsAdded = 0;

            foreach (var part in parts)
            {
                var partInDb = await _context.Parts.FindAsync(part.id);
                if (partInDb == null)
                {
                    throw new InvalidOperationException($"Część o ID {part.id} nie istnieje");
                }

                if (partInDb.StockQuantity < part.quantity)
                {
                    throw new InvalidOperationException(
                        $"Niewystarczająca ilość części {partInDb.Name}. Dostępne: {partInDb.StockQuantity}, Żądane: {part.quantity}");
                }

                _context.UsedParts.Add(new UsedPart
                {
                    PartId = part.id,
                    Quantity = part.quantity,
                    ServiceOrderId = orderId
                });

                partInDb.StockQuantity -= part.quantity;
                _context.Parts.Update(partInDb);

                partsAdded++;
            }

            return partsAdded;
        }
    }
}