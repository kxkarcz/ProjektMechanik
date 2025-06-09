using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkshopManager.Data;
using WorkshopManager.DTOs;
using WorkshopManager.Mappers;

namespace WorkshopManager.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _context;
        private readonly CustomerMapper _mapper = new();
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(ApplicationDbContext context, ILogger<CustomerService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<CustomerDto>> GetAllAsync(string? filter)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filter))
                {
                    _logger.LogInformation("Rozpoczęto pobieranie wszystkich klientów");
                }
                else
                {
                    _logger.LogInformation("Rozpoczęto pobieranie klientów z filtrem: '{Filter}'", filter);
                }

                var query = _context.Customers.AsQueryable();

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    query = query.Where(c => c.FirstName.Contains(filter) || c.LastName.Contains(filter));
                }

                var customers = await query.ToListAsync();
                var result = customers.Select(_mapper.ToDto).ToList();

                if (string.IsNullOrWhiteSpace(filter))
                {
                    _logger.LogInformation("Pomyślnie pobrano {Count} klientów", result.Count);
                }
                else
                {
                    _logger.LogInformation("Pomyślnie pobrano {Count} klientów z filtrem: '{Filter}'", result.Count, filter);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania klientów z filtrem: '{Filter}'", filter ?? "brak");
                throw;
            }
        }

        public async Task<CustomerDto?> GetByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto pobieranie klienta ID: {CustomerId}", id);

                var customer = await _context.Customers.FindAsync(id);

                if (customer is null)
                {
                    _logger.LogWarning("Nie znaleziono klienta o ID: {CustomerId}", id);
                    return null;
                }

                var result = _mapper.ToDto(customer);
                _logger.LogInformation("Pomyślnie pobrano klienta ID: {CustomerId}, Imię: '{FirstName}', Nazwisko: '{LastName}'",
                    id, customer.FirstName, customer.LastName);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania klienta ID: {CustomerId}", id);
                throw;
            }
        }

        public async Task CreateAsync(CustomerDto dto)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto tworzenie nowego klienta: '{FirstName} {LastName}', Email: '{Email}'",
                    dto.FirstName, dto.LastName, dto.Email);

                var customer = _mapper.FromDto(dto);
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Pomyślnie utworzono klienta ID: {CustomerId}, '{FirstName} {LastName}'",
                    customer.Id, dto.FirstName, dto.LastName);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Błąd bazy danych podczas tworzenia klienta: '{FirstName} {LastName}', Email: '{Email}'",
                    dto.FirstName, dto.LastName, dto.Email);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas tworzenia klienta: '{FirstName} {LastName}', Email: '{Email}'",
                    dto.FirstName, dto.LastName, dto.Email);
                throw;
            }
        }

        public async Task UpdateAsync(CustomerDto dto)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto aktualizację klienta ID: {CustomerId}", dto.Id);

                var customer = await _context.Customers.FindAsync(dto.Id);
                if (customer is null)
                {
                    _logger.LogWarning("Nie znaleziono klienta o ID: {CustomerId} do aktualizacji", dto.Id);
                    return;
                }

                var oldData = $"{customer.FirstName} {customer.LastName} ({customer.Email})";

                customer.FirstName = dto.FirstName;
                customer.LastName = dto.LastName;
                customer.Email = dto.Email;
                customer.PhoneNumber = dto.PhoneNumber;

                await _context.SaveChangesAsync();

                var newData = $"{dto.FirstName} {dto.LastName} ({dto.Email})";
                _logger.LogInformation("Pomyślnie zaktualizowano klienta ID: {CustomerId}. Zmiana z '{OldData}' na '{NewData}'",
                    dto.Id, oldData, newData);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Błąd bazy danych podczas aktualizacji klienta ID: {CustomerId}", dto.Id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas aktualizacji klienta ID: {CustomerId}", dto.Id);
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto usuwanie klienta ID: {CustomerId}", id);

                var customer = await _context.Customers.FindAsync(id);
                if (customer is null)
                {
                    _logger.LogWarning("Nie znaleziono klienta o ID: {CustomerId} do usunięcia", id);
                    return;
                }

                var customerInfo = $"{customer.FirstName} {customer.LastName} ({customer.Email})";
                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Pomyślnie usunięto klienta ID: {CustomerId}, Dane: '{CustomerInfo}'",
                    id, customerInfo);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Błąd bazy danych podczas usuwania klienta ID: {CustomerId}", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas usuwania klienta ID: {CustomerId}", id);
                throw;
            }
        }
    }
}