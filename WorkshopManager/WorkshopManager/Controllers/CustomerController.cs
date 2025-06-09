using Microsoft.AspNetCore.Mvc;
using WorkshopManager.Models;
using WorkshopManager.Data;
using WorkshopManager.Services;
using WorkshopManager.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using WorkshopManager.Extensions;
using System.Security.Claims;


namespace WorkshopManager.Controllers
{
    [Authorize(Policy = "AdminOrRecepcjonista")]
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICustomerService _customerService;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(ApplicationDbContext context, ICustomerService customerService, ILogger<CustomerController> logger)
        {
            _context = context;
            _customerService = customerService;
            _logger = logger;
        }

        // GET: Customers
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Rozpoczęcie pobierania listy wszystkich klientów");

            try
            {
                var customers = await _context.Customers
                    .Include(c => c.Vehicles)
                    .ToListAsync();

                _logger.LogInformation("Pobrano {Count} klientów wraz z {VehicleCount} pojazdami",
                    customers.Count, customers.SelectMany(c => c.Vehicles).Count());

                return View(customers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania listy klientów");
                TempData["ErrorMessage"] = "Wystąpił błąd podczas pobierania listy klientów.";
                return View(new List<Customer>());
            }
        }

        // GET: Customers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            _logger.LogInformation("Rozpoczęcie pobierania szczegółów klienta o ID: {CustomerId}", id);

            if (id == null)
            {
                _logger.LogWarning("Nie podano ID klienta w szczegółach");
                return NotFound();
            }

            try
            {
                var customer = await _context.Customers
                    .Include(c => c.Vehicles)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (customer == null)
                {
                    _logger.LogWarning("Nie znaleziono klienta o ID: {CustomerId}", id);
                    return NotFound();
                }

                _logger.LogInformation("Pobrano szczegóły klienta: {CustomerName} {CustomerSurname} (ID: {CustomerId}) z {VehicleCount} pojazdami",
                    customer.FirstName, customer.LastName, customer.Id, customer.Vehicles.Count);

                return View(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania szczegółów klienta o ID: {CustomerId}", id);
                return NotFound();
            }
        }

        // GET: Customers/Create
        public IActionResult Create()
        {
            _logger.LogInformation("Wyświetlanie formularza tworzenia nowego klienta");
            return View(new CustomerDto());
        }

        // POST: Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerDto customerDto)
        {
            _logger.LogInformation("Rozpoczęcie tworzenia nowego klienta: {CustomerName} {CustomerSurname}, Email: {Email}",
                customerDto.FirstName, customerDto.LastName, customerDto.Email);

            if (ModelState.IsValid)
            {
                try
                {
                    await _customerService.CreateAsync(customerDto);

                    _logger.LogInformation("Pomyślnie utworzono klienta: {CustomerName} {CustomerSurname}, Email: {Email}",
                        customerDto.FirstName, customerDto.LastName, customerDto.Email);

                    TempData["SuccessMessage"] = $"Klient {customerDto.FirstName} {customerDto.LastName} został pomyślnie utworzony.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Błąd podczas tworzenia klienta: {CustomerName} {CustomerSurname}, Email: {Email}",
                        customerDto.FirstName, customerDto.LastName, customerDto.Email);

                    ModelState.AddModelError("", "Wystąpił błąd podczas zapisywania klienta: " + ex.Message);
                }
            }
            else
            {
                _logger.LogWarning("Model klienta nie jest prawidłowy. Błędy: {Errors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            }

            return View(customerDto);
        }

        // GET: Customers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            _logger.LogInformation("Wyświetlanie formularza edycji klienta o ID: {CustomerId}", id);

            if (id == null)
            {
                _logger.LogWarning("Nie podano ID klienta do edycji");
                return NotFound();
            }

            try
            {
                var customerDto = await _customerService.GetByIdAsync(id.Value);
                if (customerDto == null)
                {
                    _logger.LogWarning("Nie znaleziono klienta o ID: {CustomerId} do edycji", id);
                    return NotFound();
                }

                _logger.LogInformation("Załadowano dane klienta do edycji: {CustomerName} {CustomerSurname} (ID: {CustomerId})",
                    customerDto.FirstName, customerDto.LastName, customerDto.Id);

                return View(customerDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas ładowania danych klienta o ID: {CustomerId} do edycji", id);
                return NotFound();
            }
        }

        // POST: Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CustomerDto customerDto)
        {
            _logger.LogInformation("Rozpoczęcie aktualizacji klienta o ID: {CustomerId} - {CustomerName} {CustomerSurname}",
                id, customerDto.FirstName, customerDto.LastName);

            if (id != customerDto.Id)
            {
                _logger.LogWarning("Niezgodność ID klienta: URL ID: {UrlId}, DTO ID: {DtoId}", id, customerDto.Id);
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _customerService.UpdateAsync(customerDto);

                    _logger.LogInformation("Pomyślnie zaktualizowano klienta: {CustomerName} {CustomerSurname} (ID: {CustomerId})",
                        customerDto.FirstName, customerDto.LastName, customerDto.Id);

                    TempData["SuccessMessage"] = $"Dane klienta {customerDto.FirstName} {customerDto.LastName} zostały pomyślnie zaktualizowane.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Błąd podczas aktualizacji klienta: {CustomerName} {CustomerSurname} (ID: {CustomerId})",
                        customerDto.FirstName, customerDto.LastName, customerDto.Id);

                    ModelState.AddModelError("", "Wystąpił błąd podczas aktualizacji klienta: " + ex.Message);
                }
            }
            else
            {
                _logger.LogWarning("Model klienta nie jest prawidłowy podczas edycji ID: {CustomerId}. Błędy: {Errors}",
                    id, string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            }

            return View(customerDto);
        }

        // GET: Customers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            _logger.LogInformation("Wyświetlanie potwierdzenia usunięcia klienta o ID: {CustomerId}", id);

            if (id == null)
            {
                _logger.LogWarning("Nie podano ID klienta do usunięcia");
                return NotFound();
            }

            try
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (customer == null)
                {
                    _logger.LogWarning("Nie znaleziono klienta o ID: {CustomerId} do usunięcia", id);
                    return NotFound();
                }

                _logger.LogInformation("Załadowano klienta do potwierdzenia usunięcia: {CustomerName} {CustomerSurname} (ID: {CustomerId})",
                    customer.FirstName, customer.LastName, customer.Id);

                return View(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas ładowania klienta o ID: {CustomerId} do usunięcia", id);
                return NotFound();
            }
        }

        // POST: Customers/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            _logger.LogInformation("Rozpoczęcie usuwania klienta o ID: {CustomerId}", id);

            try
            {
                // Najpierw pobieramy dane klienta do logowania
                var customerDto = await _customerService.GetByIdAsync(id);
                if (customerDto != null)
                {
                    _logger.LogInformation("Usuwanie klienta: {CustomerName} {CustomerSurname} (ID: {CustomerId})",
                        customerDto.FirstName, customerDto.LastName, id);
                }

                await _customerService.DeleteAsync(id);

                _logger.LogInformation("Pomyślnie usunięto klienta o ID: {CustomerId}", id);
                TempData["SuccessMessage"] = "Klient został pomyślnie usunięty.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas usuwania klienta o ID: {CustomerId}", id);
                TempData["Error"] = "Wystąpił błąd podczas usuwania klienta: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CustomerExists(int id)
        {
            try
            {
                var exists = _context.Customers.Any(e => e.Id == id);
                _logger.LogDebug("Sprawdzenie istnienia klienta o ID: {CustomerId} - rezultat: {Exists}", id, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas sprawdzania istnienia klienta o ID: {CustomerId}", id);
                return false;
            }
        }
    }
}