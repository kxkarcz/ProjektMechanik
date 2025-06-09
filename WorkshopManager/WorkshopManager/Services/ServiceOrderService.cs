using WorkshopManager.Data;
using WorkshopManager.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace WorkshopManager.Services
{
    public class ServiceOrderService : IServiceOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly ServiceOrderMapper _mapper;
        private readonly IPdfReportService _pdfService;
        private readonly ILogger<ServiceOrderService> _logger;

        public ServiceOrderService(ApplicationDbContext context, ServiceOrderMapper mapper, IPdfReportService pdfService, ILogger<ServiceOrderService> logger)
        {
            _context = context;
            _mapper = mapper;
            _pdfService = pdfService;
            _logger = logger;
        }

        public async Task<List<ServiceOrderDto>> GetAllOrdersAsync()
        {
            try
            {
                _logger.LogInformation("Rozpoczęto pobieranie wszystkich zleceń serwisowych");

                var orders = await _context.ServiceOrders
                    .Include(o => o.ServiceTasks)
                    .Include(o => o.Comments)
                    .ToListAsync();

                var result = orders.Select(_mapper.ToDto).ToList();

                _logger.LogInformation("Pomyślnie pobrano {Count} zleceń serwisowych", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania wszystkich zleceń serwisowych");
                throw;
            }
        }

        public async Task<ServiceOrderDto?> GetOrderByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto pobieranie zlecenia serwisowego ID: {OrderId}", id);

                var order = await _context.ServiceOrders
                    .Include(o => o.ServiceTasks)
                    .Include(o => o.Comments)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    _logger.LogWarning("Nie znaleziono zlecenia serwisowego o ID: {OrderId}", id);
                    return null;
                }

                var result = _mapper.ToDto(order);
                _logger.LogInformation("Pomyślnie pobrano zlecenie ID: {OrderId}, Status: '{Status}', Mechanik: '{MechanicId}'",
                    id, order.Status, order.AssignedMechanicId ?? "nieprzypisany");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania zlecenia serwisowego ID: {OrderId}", id);
                throw;
            }
        }

        public async Task<List<ServiceOrderDto>> GetOrdersByStatusAsync(string status)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto pobieranie zleceń o statusie: '{Status}'", status);

                var orders = await _context.ServiceOrders
                    .Where(o => o.Status == status)
                    .ToListAsync();

                var result = orders.Select(_mapper.ToDto).ToList();

                _logger.LogInformation("Pobrano {Count} zleceń o statusie: '{Status}'", result.Count, status);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania zleceń o statusie: '{Status}'", status);
                throw;
            }
        }

        public async Task<List<ServiceOrderDto>> GetOrdersByMechanicIdAsync(string mechanicId)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto pobieranie zleceń mechanika: '{MechanicId}'", mechanicId);

                var orders = await _context.ServiceOrders
                    .Where(o => o.AssignedMechanicId == mechanicId)
                    .ToListAsync();

                var result = orders.Select(_mapper.ToDto).ToList();

                _logger.LogInformation("Pobrano {Count} zleceń przypisanych do mechanika: '{MechanicId}'", result.Count, mechanicId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania zleceń mechanika: '{MechanicId}'", mechanicId);
                throw;
            }
        }

        public async Task<List<ServiceOrderDto>> GetOrdersByVehicleIdAsync(int vehicleId)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto pobieranie zleceń dla pojazdu ID: {VehicleId}", vehicleId);

                var orders = await _context.ServiceOrders
                    .Where(o => o.VehicleId == vehicleId)
                    .ToListAsync();

                var result = orders.Select(_mapper.ToDto).ToList();

                _logger.LogInformation("Pobrano {Count} zleceń dla pojazdu ID: {VehicleId}", result.Count, vehicleId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania zleceń dla pojazdu ID: {VehicleId}", vehicleId);
                throw;
            }
        }

        public async Task<List<ServiceOrderDto>> GetOrdersByCustomerIdAsync(int customerId)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto pobieranie zleceń dla klienta ID: {CustomerId}", customerId);

                var orders = await _context.ServiceOrders
                    .Include(o => o.Vehicle)
                    .Where(o => o.Vehicle.CustomerId == customerId)
                    .ToListAsync();

                var result = orders.Select(_mapper.ToDto).ToList();

                _logger.LogInformation("Pobrano {Count} zleceń dla klienta ID: {CustomerId}", result.Count, customerId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania zleceń dla klienta ID: {CustomerId}", customerId);
                throw;
            }
        }

        public async Task<ServiceOrderDto> CreateOrderAsync(ServiceOrderCreateDto orderDto)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto tworzenie nowego zlecenia serwisowego dla pojazdu ID: {VehicleId}, Status: '{Status}'",
                    orderDto.VehicleId, orderDto.Status);

                var order = _mapper.ToEntity(orderDto);
                _context.ServiceOrders.Add(order);
                await _context.SaveChangesAsync();

                var result = _mapper.ToDto(order);

                _logger.LogInformation("Pomyślnie utworzono zlecenie serwisowe ID: {OrderId} dla pojazdu ID: {VehicleId}",
                    order.Id, orderDto.VehicleId);

                return result;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Błąd bazy danych podczas tworzenia zlecenia serwisowego dla pojazdu ID: {VehicleId}", orderDto.VehicleId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas tworzenia zlecenia serwisowego dla pojazdu ID: {VehicleId}", orderDto.VehicleId);
                throw;
            }
        }

        public async Task<ServiceOrderDto?> UpdateOrderAsync(int id, ServiceOrderUpdateDto orderDto)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto aktualizację zlecenia serwisowego ID: {OrderId}", id);

                var existing = await _context.ServiceOrders.FindAsync(id);
                if (existing == null)
                {
                    _logger.LogWarning("Nie znaleziono zlecenia serwisowego o ID: {OrderId} do aktualizacji", id);
                    return null;
                }

                var oldStatus = existing.Status;
                var oldMechanicId = existing.AssignedMechanicId;

                _mapper.UpdateEntity(orderDto, existing);
                await _context.SaveChangesAsync();

                var result = _mapper.ToDto(existing);

                _logger.LogInformation("Pomyślnie zaktualizowano zlecenie ID: {OrderId}. Status: '{OldStatus}' -> '{NewStatus}', " +
                    "Mechanik: '{OldMechanicId}' -> '{NewMechanicId}'",
                    id, oldStatus, existing.Status, oldMechanicId ?? "brak", existing.AssignedMechanicId ?? "brak");

                return result;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Błąd bazy danych podczas aktualizacji zlecenia serwisowego ID: {OrderId}", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas aktualizacji zlecenia serwisowego ID: {OrderId}", id);
                throw;
            }
        }

        public async Task<ServiceOrderDto?> UpdateOrderStatusAsync(int id, string newStatus)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto zmianę statusu zlecenia ID: {OrderId} na '{NewStatus}'", id, newStatus);

                var order = await _context.ServiceOrders.FindAsync(id);
                if (order == null)
                {
                    _logger.LogWarning("Nie znaleziono zlecenia o ID: {OrderId} do zmiany statusu", id);
                    return null;
                }

                var oldStatus = order.Status;
                order.Status = newStatus;
                await _context.SaveChangesAsync();

                var result = _mapper.ToDto(order);

                _logger.LogInformation("Zmieniono status zlecenia ID: {OrderId} z '{OldStatus}' na '{NewStatus}'",
                    id, oldStatus, newStatus);

                // Logowanie ważnych zmian statusu
                if (newStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Zlecenie ID: {OrderId} zostało UKOŃCZONE", id);
                }
                else if (newStatus.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Zlecenie ID: {OrderId} zostało ANULOWANE", id);
                }

                return result;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Błąd bazy danych podczas zmiany statusu zlecenia ID: {OrderId} na '{NewStatus}'", id, newStatus);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas zmiany statusu zlecenia ID: {OrderId} na '{NewStatus}'", id, newStatus);
                throw;
            }
        }

        public async Task<bool> DeleteOrderAsync(int id)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto usuwanie zlecenia serwisowego ID: {OrderId}", id);

                var order = await _context.ServiceOrders.FindAsync(id);
                if (order == null)
                {
                    _logger.LogWarning("Nie znaleziono zlecenia o ID: {OrderId} do usunięcia", id);
                    return false;
                }

                var orderInfo = $"Status: {order.Status}, Pojazd: {order.VehicleId}, Mechanik: {order.AssignedMechanicId ?? "nieprzypisany"}";
                _context.ServiceOrders.Remove(order);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Pomyślnie usunięto zlecenie ID: {OrderId} ({OrderInfo})", id, orderInfo);
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Błąd bazy danych podczas usuwania zlecenia ID: {OrderId}. " +
                    "Możliwe że zlecenie ma powiązane zadania lub komentarze", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas usuwania zlecenia ID: {OrderId}", id);
                throw;
            }
        }

        public async Task<ServiceOrderDto?> AssignMechanicAsync(int orderId, string mechanicId)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto przypisywanie mechanika '{MechanicId}' do zlecenia ID: {OrderId}", mechanicId, orderId);

                var order = await _context.ServiceOrders.FindAsync(orderId);
                if (order == null)
                {
                    _logger.LogWarning("Nie znaleziono zlecenia o ID: {OrderId} do przypisania mechanika", orderId);
                    return null;
                }

                var oldMechanicId = order.AssignedMechanicId;
                order.AssignedMechanicId = mechanicId;
                await _context.SaveChangesAsync();

                var result = _mapper.ToDto(order);

                if (string.IsNullOrEmpty(oldMechanicId))
                {
                    _logger.LogInformation("Przypisano mechanika '{MechanicId}' do zlecenia ID: {OrderId}", mechanicId, orderId);
                }
                else
                {
                    _logger.LogInformation("Zmieniono mechanika zlecenia ID: {OrderId} z '{OldMechanicId}' na '{NewMechanicId}'",
                        orderId, oldMechanicId, mechanicId);
                }

                return result;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Błąd bazy danych podczas przypisywania mechanika '{MechanicId}' do zlecenia ID: {OrderId}",
                    mechanicId, orderId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas przypisywania mechanika '{MechanicId}' do zlecenia ID: {OrderId}",
                    mechanicId, orderId);
                throw;
            }
        }

        public async Task<byte[]?> GenerateOrderPdfAsync(int orderId)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto generowanie PDF dla zlecenia ID: {OrderId}", orderId);

                var pdf = await _pdfService.GenerateServiceOrderReportAsync(orderId);

                if (pdf == null || pdf.Length == 0)
                {
                    _logger.LogWarning("Wygenerowany PDF dla zlecenia ID: {OrderId} jest pusty lub null", orderId);
                    return null;
                }

                _logger.LogInformation("Pomyślnie wygenerowano PDF dla zlecenia ID: {OrderId}. Rozmiar: {PdfSize} bajtów",
                    orderId, pdf.Length);

                return pdf;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas generowania PDF dla zlecenia ID: {OrderId}", orderId);
                throw;
            }
        }
    }
}