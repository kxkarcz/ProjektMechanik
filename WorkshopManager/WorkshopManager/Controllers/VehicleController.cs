using Microsoft.AspNetCore.Mvc;
using WorkshopManager.Models;
using WorkshopManager.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using WorkshopManager.Extensions;
using System.Security.Claims;

namespace WorkshopManager.Controllers
{
    [Authorize(Policy = "AdminOrRecepcjonista")]
    public class VehicleController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VehicleController> _logger;

        public VehicleController(ApplicationDbContext context, ILogger<VehicleController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Vehicles
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Rozpoczęcie pobierania listy wszystkich pojazdów");

            try
            {
                var vehicles = await _context.Vehicles
                    .Include(v => v.Customer)
                    .ToListAsync();

                var vehiclesPerCustomer = vehicles
                    .GroupBy(v => v.Customer)
                    .ToDictionary(g => $"{g.Key?.FirstName} {g.Key?.LastName}".Trim(), g => g.Count());

                var avgVehiclesPerCustomer = vehiclesPerCustomer.Any() ? vehiclesPerCustomer.Values.Average() : 0;
                var customersWithMultipleVehicles = vehiclesPerCustomer.Count(kv => kv.Value > 1);

                _logger.LogInformation("Pobrano {VehicleCount} pojazdów należących do {CustomerCount} klientów. Średnio {AvgVehicles:F1} pojazdów na klienta, {MultipleVehicleCustomers} klientów ma więcej niż 1 pojazd",
                    vehicles.Count, vehiclesPerCustomer.Count, avgVehiclesPerCustomer, customersWithMultipleVehicles);

                if (vehiclesPerCustomer.Any())
                {
                    var topCustomer = vehiclesPerCustomer.OrderByDescending(kv => kv.Value).First();
                    _logger.LogDebug("Klient z największą liczbą pojazdów: {CustomerName} ({VehicleCount} pojazdów)",
                        topCustomer.Key, topCustomer.Value);
                }

                return View(vehicles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania listy pojazdów");
                TempData["ErrorMessage"] = "Wystąpił błąd podczas pobierania pojazdów.";
                return View(new List<Vehicle>());
            }
        }

        // GET: Vehicles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            _logger.LogInformation("Rozpoczęcie pobierania szczegółów pojazdu o ID: {VehicleId}", id);

            if (id == null)
            {
                _logger.LogWarning("Nie podano ID pojazdu w szczegółach");
                return NotFound();
            }

            try
            {
                var vehicle = await _context.Vehicles
                    .Include(v => v.Customer)
                    .Include(v => v.ServiceOrders)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (vehicle == null)
                {
                    _logger.LogWarning("Nie znaleziono pojazdu o ID: {VehicleId}", id);
                    return NotFound();
                }

                var serviceOrdersCount = vehicle.ServiceOrders?.Count ?? 0;
                var activeOrdersCount = vehicle.ServiceOrders?.Count(so => so.Status != "Zakończone") ?? 0;

                _logger.LogInformation("Pobrano szczegóły pojazdu ID: {VehicleId}, VIN: {VIN}, Rejestracja: {Registration}, Właściciel: {OwnerName}, Zlecenia serwisowe: {ServiceOrdersCount} (aktywne: {ActiveOrdersCount})",
                    vehicle.Id, vehicle.VIN, vehicle.RegistrationNumber,
                    $"{vehicle.Customer?.FirstName} {vehicle.Customer?.LastName}".Trim(),
                    serviceOrdersCount, activeOrdersCount);

                return View(vehicle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania szczegółów pojazdu o ID: {VehicleId}", id);
                return NotFound();
            }
        }

        // GET: Vehicles/Create
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("Wyświetlanie formularza dodawania nowego pojazdu");

            try
            {
                var customers = await _context.Customers.ToListAsync();
                ViewBag.Customers = new SelectList(customers.Select(c => new {
                    Id = c.Id,
                    FullName = $"{c.FirstName} {c.LastName}"
                }), "Id", "FullName");

                _logger.LogDebug("Załadowano {CustomerCount} klientów do wyboru dla nowego pojazdu", customers.Count);
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas ładowania listy klientów dla nowego pojazdu");
                ViewBag.Customers = new SelectList(new List<object>(), "Id", "FullName");
                TempData["ErrorMessage"] = "Wystąpił błąd podczas ładowania klientów.";
                return View();
            }
        }

        // POST: Vehicles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("VIN,RegistrationNumber,CustomerId,ImageUrl")] Vehicle vehicle, IFormFile? vehicleImage)
        {
            _logger.LogInformation("Rozpoczęcie dodawania nowego pojazdu: VIN: {VIN}, Rejestracja: {Registration}, Klient ID: {CustomerId}",
                vehicle.VIN, vehicle.RegistrationNumber, vehicle.CustomerId);

            ModelState.Remove("Customer");
            ModelState.Remove("ServiceOrders");
            ModelState.Remove("ImageUrl");

            if (ModelState.IsValid)
            {
                try
                {
                    var customer = await _context.Customers.FindAsync(vehicle.CustomerId);
                    if (customer == null)
                    {
                        _logger.LogWarning("Próba dodania pojazdu z nieistniejącym klientem ID: {CustomerId}", vehicle.CustomerId);
                        ModelState.AddModelError("CustomerId", "Wybrany klient nie istnieje.");
                        await LoadCustomersSelectList(vehicle.CustomerId);
                        return View(vehicle);
                    }

                    var existingVehicleWithVin = await _context.Vehicles.FirstOrDefaultAsync(v => v.VIN == vehicle.VIN);
                    if (existingVehicleWithVin != null)
                    {
                        _logger.LogWarning("Próba dodania pojazdu z już istniejącym VIN: {VIN} (istniejący pojazd ID: {ExistingVehicleId})",
                            vehicle.VIN, existingVehicleWithVin.Id);
                        ModelState.AddModelError("VIN", "Pojazd z tym numerem VIN już istnieje w systemie.");
                        await LoadCustomersSelectList(vehicle.CustomerId);
                        return View(vehicle);
                    }

                    var existingVehicleWithRegistration = await _context.Vehicles.FirstOrDefaultAsync(v => v.RegistrationNumber == vehicle.RegistrationNumber);
                    if (existingVehicleWithRegistration != null)
                    {
                        _logger.LogWarning("Próba dodania pojazdu z już istniejącym numerem rejestracyjnym: {Registration} (istniejący pojazd ID: {ExistingVehicleId})",
                            vehicle.RegistrationNumber, existingVehicleWithRegistration.Id);
                        ModelState.AddModelError("RegistrationNumber", "Pojazd z tym numerem rejestracyjnym już istnieje w systemie.");
                        await LoadCustomersSelectList(vehicle.CustomerId);
                        return View(vehicle);
                    }

                    if (vehicleImage != null && vehicleImage.Length > 0)
                    {
                        var imageUrl = await SaveVehicleImageAsync(vehicleImage);
                        vehicle.ImageUrl = imageUrl;
                    }
                    else
                    {
                        vehicle.ImageUrl = string.Empty;
                    }

                    _context.Add(vehicle);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Pomyślnie dodano pojazd ID: {VehicleId}, VIN: {VIN}, Rejestracja: {Registration}, Właściciel: {OwnerName} (ID: {CustomerId})",
                        vehicle.Id, vehicle.VIN, vehicle.RegistrationNumber,
                        $"{customer.FirstName} {customer.LastName}".Trim(), vehicle.CustomerId);

                    TempData["SuccessMessage"] = $"Pojazd {vehicle.RegistrationNumber} został pomyślnie dodany.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Błąd podczas dodawania pojazdu: VIN: {VIN}, Rejestracja: {Registration}, Klient ID: {CustomerId}",
                        vehicle.VIN, vehicle.RegistrationNumber, vehicle.CustomerId);

                    ModelState.AddModelError("", "Wystąpił błąd podczas dodawania pojazdu.");
                }
            }
            else
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { Field = x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage) });

                _logger.LogWarning("Model pojazdu nie jest prawidłowy. VIN: {VIN}, Błędy: {Errors}",
                    vehicle.VIN, string.Join(", ", errors.SelectMany(e => e.Errors)));

                foreach (var error in errors)
                {
                    _logger.LogDebug("Pole: {Field}, Błędy: {FieldErrors}", error.Field, string.Join(", ", error.Errors));
                }
            }

            await LoadCustomersSelectList(vehicle.CustomerId);
            return View(vehicle);
        }

        private async Task LoadCustomersSelectList(int? selectedCustomerId = null)
        {
            try
            {
                var customers = await _context.Customers.ToListAsync();
                ViewBag.Customers = new SelectList(customers.Select(c => new {
                    Id = c.Id,
                    FullName = $"{c.FirstName} {c.LastName}"
                }), "Id", "FullName", selectedCustomerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas ładowania listy klientów");
                ViewBag.Customers = new SelectList(new List<object>(), "Id", "FullName");
            }
        }

        // GET: Vehicles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            _logger.LogInformation("Wyświetlanie formularza edycji pojazdu o ID: {VehicleId}", id);

            if (id == null)
            {
                _logger.LogWarning("Nie podano ID pojazdu do edycji");
                return NotFound();
            }

            try
            {
                var vehicle = await _context.Vehicles.FindAsync(id);
                if (vehicle == null)
                {
                    _logger.LogWarning("Nie znaleziono pojazdu o ID: {VehicleId} do edycji", id);
                    return NotFound();
                }

                await LoadCustomersSelectList(vehicle.CustomerId);

                _logger.LogInformation("Załadowano pojazd do edycji: ID: {VehicleId}, VIN: {VIN}, Rejestracja: {Registration}, Klient ID: {CustomerId}",
                    vehicle.Id, vehicle.VIN, vehicle.RegistrationNumber, vehicle.CustomerId);

                return View(vehicle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas ładowania pojazdu o ID: {VehicleId} do edycji", id);
                return NotFound();
            }
        }

        // POST: Vehicles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,VIN,RegistrationNumber,CustomerId,ImageUrl")] Vehicle vehicle, IFormFile? vehicleImage)
        {
            _logger.LogInformation("Rozpoczęcie aktualizacji pojazdu o ID: {VehicleId}, Nowy VIN: {VIN}, Nowa rejestracja: {Registration}",
                id, vehicle.VIN, vehicle.RegistrationNumber);

            if (id != vehicle.Id)
            {
                _logger.LogWarning("Niezgodność ID pojazdu: URL ID: {UrlId}, Model ID: {ModelId}", id, vehicle.Id);
                return NotFound();
            }

            ModelState.Remove("Customer");
            ModelState.Remove("ServiceOrders");

            if (ModelState.IsValid)
            {
                try
                {
                    var originalVehicle = await _context.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
                    var customer = await _context.Customers.FindAsync(vehicle.CustomerId);

                    if (originalVehicle != null && originalVehicle.VIN != vehicle.VIN)
                    {
                        var existingVehicleWithVin = await _context.Vehicles.FirstOrDefaultAsync(v => v.VIN == vehicle.VIN && v.Id != id);
                        if (existingVehicleWithVin != null)
                        {
                            _logger.LogWarning("Próba aktualizacji pojazdu ID: {VehicleId} z już istniejącym VIN: {VIN} (konflikt z pojazdem ID: {ExistingVehicleId})",
                                id, vehicle.VIN, existingVehicleWithVin.Id);
                            ModelState.AddModelError("VIN", "Pojazd z tym numerem VIN już istnieje w systemie.");
                            await LoadCustomersSelectList(vehicle.CustomerId);
                            return View(vehicle);
                        }
                    }

                    if (vehicleImage != null && vehicleImage.Length > 0)
                    {
                        var imageUrl = await SaveVehicleImageAsync(vehicleImage);
                        vehicle.ImageUrl = imageUrl;
                    }
                    else if (originalVehicle != null)
                    {
                        vehicle.ImageUrl = originalVehicle.ImageUrl;
                    }

                    _context.Update(vehicle);
                    await _context.SaveChangesAsync();

                    if (originalVehicle != null)
                    {
                        _logger.LogInformation("Pomyślnie zaktualizowano pojazd ID: {VehicleId}. Zmiany: VIN: {OldVIN} → {NewVIN}, Rejestracja: {OldRegistration} → {NewRegistration}, Klient ID: {OldCustomerId} → {NewCustomerId}",
                            id, originalVehicle.VIN, vehicle.VIN, originalVehicle.RegistrationNumber, vehicle.RegistrationNumber, originalVehicle.CustomerId, vehicle.CustomerId);
                    }
                    else
                    {
                        _logger.LogInformation("Pomyślnie zaktualizowano pojazd ID: {VehicleId}, VIN: {VIN}, Rejestracja: {Registration}, Właściciel: {OwnerName}",
                            id, vehicle.VIN, vehicle.RegistrationNumber, $"{customer?.FirstName} {customer?.LastName}".Trim());
                    }

                    TempData["SuccessMessage"] = $"Pojazd {vehicle.RegistrationNumber} został pomyślnie zaktualizowany.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Błąd współbieżności podczas aktualizacji pojazdu o ID: {VehicleId}", id);

                    if (!VehicleExists(vehicle.Id))
                    {
                        _logger.LogWarning("Pojazd o ID: {VehicleId} został usunięty przez innego użytkownika", id);
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Błąd podczas aktualizacji pojazdu o ID: {VehicleId}", id);
                    ModelState.AddModelError("", "Wystąpił błąd podczas aktualizacji pojazdu.");
                }
            }
            else
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { Field = x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage) });

                _logger.LogWarning("Model pojazdu nie jest prawidłowy podczas edycji ID: {VehicleId}. Błędy: {Errors}",
                    id, string.Join(", ", errors.SelectMany(e => e.Errors)));
            }

            await LoadCustomersSelectList(vehicle.CustomerId);
            return View(vehicle);
        }

        // GET: Vehicles/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            _logger.LogInformation("Wyświetlanie potwierdzenia usunięcia pojazdu o ID: {VehicleId}", id);

            if (id == null)
            {
                _logger.LogWarning("Nie podano ID pojazdu do usunięcia");
                return NotFound();
            }

            try
            {
                var vehicle = await _context.Vehicles
                    .Include(v => v.Customer)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (vehicle == null)
                {
                    _logger.LogWarning("Nie znaleziono pojazdu o ID: {VehicleId} do usunięcia", id);
                    return NotFound();
                }

                _logger.LogInformation("Załadowano pojazd do potwierdzenia usunięcia: ID: {VehicleId}, VIN: {VIN}, Rejestracja: {Registration}, Właściciel: {OwnerName}",
                    vehicle.Id, vehicle.VIN, vehicle.RegistrationNumber,
                    $"{vehicle.Customer?.FirstName} {vehicle.Customer?.LastName}".Trim());

                return View(vehicle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas ładowania pojazdu o ID: {VehicleId} do usunięcia", id);
                return NotFound();
            }
        }

        // POST: Vehicles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            _logger.LogInformation("Rozpoczęcie usuwania pojazdu o ID: {VehicleId}", id);

            try
            {
                var vehicle = await _context.Vehicles
                    .Include(v => v.Customer)
                    .Include(v => v.ServiceOrders)
                    .FirstOrDefaultAsync(v => v.Id == id);

                if (vehicle != null)
                {
                    var serviceOrdersCount = vehicle.ServiceOrders?.Count ?? 0;
                    var activeOrdersCount = vehicle.ServiceOrders?.Count(so => so.Status != "Zakończone") ?? 0;

                    if (activeOrdersCount > 0)
                    {
                        _logger.LogWarning("Próba usunięcia pojazdu ID: {VehicleId} z aktywnymi zleceniami serwisowymi ({ActiveOrders} aktywnych z {TotalOrders} łącznie)",
                            id, activeOrdersCount, serviceOrdersCount);

                        TempData["ErrorMessage"] = $"Nie można usunąć pojazdu {vehicle.RegistrationNumber} - ma aktywne zlecenia serwisowe.";
                        return RedirectToAction(nameof(Index));
                    }

                    _logger.LogInformation("Usuwanie pojazdu: ID: {VehicleId}, VIN: {VIN}, Rejestracja: {Registration}, Właściciel: {OwnerName}, Historia zleceń: {ServiceOrdersCount}",
                        vehicle.Id, vehicle.VIN, vehicle.RegistrationNumber,
                        $"{vehicle.Customer?.FirstName} {vehicle.Customer?.LastName}".Trim(), serviceOrdersCount);

                    _context.Vehicles.Remove(vehicle);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Pomyślnie usunięto pojazd o ID: {VehicleId}", id);
                    TempData["SuccessMessage"] = $"Pojazd {vehicle.RegistrationNumber} został pomyślnie usunięty.";
                }
                else
                {
                    _logger.LogWarning("Nie znaleziono pojazdu o ID: {VehicleId} do usunięcia", id);
                    TempData["WarningMessage"] = "Pojazd nie został znaleziony.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas usuwania pojazdu o ID: {VehicleId}", id);
                TempData["ErrorMessage"] = "Wystąpił błąd podczas usuwania pojazdu.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SaveVehicleImageAsync(IFormFile image)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "vehicles");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            return $"/uploads/vehicles/{fileName}";
        }

        private bool VehicleExists(int id)
        {
            try
            {
                var exists = _context.Vehicles.Any(e => e.Id == id);
                _logger.LogDebug("Sprawdzenie istnienia pojazdu o ID: {VehicleId} - rezultat: {Exists}", id, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas sprawdzania istnienia pojazdu o ID: {VehicleId}", id);
                return false;
            }
        }
    }
}