using WorkshopManager.DTOs;
using WorkshopManager.Mappers;
using WorkshopManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkshopManager.Data;
using System.IO;

namespace WorkshopManager.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly ApplicationDbContext _context;
        private readonly VehicleMapper _mapper;
        private readonly ILogger<VehicleService> _logger;

        public VehicleService(ApplicationDbContext context, VehicleMapper mapper, ILogger<VehicleService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<List<VehicleDto>> GetAllVehiclesAsync()
        {
            try
            {
                _logger.LogInformation("Rozpoczęto pobieranie wszystkich pojazdów");

                var result = await _context.Vehicles
                    .Select(v => _mapper.ToDto(v))
                    .ToListAsync();

                _logger.LogInformation("Pomyślnie pobrano {Count} pojazdów", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania wszystkich pojazdów");
                throw;
            }
        }

        public async Task<VehicleDto?> GetVehicleByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto pobieranie pojazdu ID: {VehicleId}", id);

                var result = await _context.Vehicles
                    .Where(v => v.Id == id)
                    .Select(v => _mapper.ToDto(v))
                    .FirstOrDefaultAsync();

                if (result == null)
                {
                    _logger.LogWarning("Nie znaleziono pojazdu o ID: {VehicleId}", id);
                }
                else
                {
                    _logger.LogInformation("Pomyślnie pobrano pojazd ID: {VehicleId}", id);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania pojazdu ID: {VehicleId}", id);
                throw;
            }
        }

        public async Task<List<VehicleDto>> GetVehiclesByCustomerIdAsync(int customerId)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto pobieranie pojazdów dla klienta ID: {CustomerId}", customerId);

                var result = await _context.Vehicles
                    .Where(v => v.CustomerId == customerId)
                    .Select(v => _mapper.ToDto(v))
                    .ToListAsync();

                _logger.LogInformation("Pobrano {Count} pojazdów dla klienta ID: {CustomerId}", result.Count, customerId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania pojazdów dla klienta ID: {CustomerId}", customerId);
                throw;
            }
        }

        public async Task<VehicleDto> CreateVehicleAsync(VehicleCreateDto dto)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto tworzenie nowego pojazdu dla klienta ID: {CustomerId}", dto.CustomerId);

                var vehicle = _mapper.FromDto(dto);
                _context.Vehicles.Add(vehicle);
                await _context.SaveChangesAsync();

                var result = _mapper.ToDto(vehicle);

                _logger.LogInformation("Pomyślnie utworzono pojazd ID: {VehicleId} dla klienta ID: {CustomerId}",
                    vehicle.Id, dto.CustomerId);

                return result;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Błąd bazy danych podczas tworzenia pojazdu dla klienta ID: {CustomerId}", dto.CustomerId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas tworzenia pojazdu dla klienta ID: {CustomerId}", dto.CustomerId);
                throw;
            }
        }

        public async Task<VehicleDto?> UpdateVehicleAsync(int id, VehicleUpdateDto dto)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto aktualizację pojazdu ID: {VehicleId}", id);

                var vehicle = await _context.Vehicles.FindAsync(id);
                if (vehicle == null)
                {
                    _logger.LogWarning("Nie znaleziono pojazdu o ID: {VehicleId} do aktualizacji", id);
                    return null;
                }

                _mapper.UpdateEntity(dto, vehicle);
                await _context.SaveChangesAsync();

                var result = _mapper.ToDto(vehicle);

                _logger.LogInformation("Pomyślnie zaktualizowano pojazd ID: {VehicleId}", id);

                return result;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Błąd bazy danych podczas aktualizacji pojazdu ID: {VehicleId}", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas aktualizacji pojazdu ID: {VehicleId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteVehicleAsync(int id)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto usuwanie pojazdu ID: {VehicleId}", id);

                var vehicle = await _context.Vehicles.FindAsync(id);
                if (vehicle == null)
                {
                    _logger.LogWarning("Nie znaleziono pojazdu o ID: {VehicleId} do usunięcia", id);
                    return false;
                }

                _context.Vehicles.Remove(vehicle);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Pomyślnie usunięto pojazd ID: {VehicleId}", id);
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Błąd bazy danych podczas usuwania pojazdu ID: {VehicleId}. " +
                    "Pojazd może mieć powiązane zlecenia", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas usuwania pojazdu ID: {VehicleId}", id);
                throw;
            }
        }

        public async Task<string?> UploadVehicleImageAsync(int vehicleId, IFormFile image)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto przesyłanie zdjęcia dla pojazdu ID: {VehicleId}. " +
                    "Nazwa pliku: '{FileName}', Rozmiar: {FileSize} bajtów",
                    vehicleId, image?.FileName ?? "brak", image?.Length ?? 0);

                var vehicle = await _context.Vehicles.FindAsync(vehicleId);
                if (vehicle == null)
                {
                    _logger.LogWarning("Nie znaleziono pojazdu o ID: {VehicleId} do przesłania zdjęcia", vehicleId);
                    return null;
                }

                if (image == null || image.Length == 0)
                {
                    _logger.LogWarning("Przesłano nieprawidłowy plik zdjęcia dla pojazdu ID: {VehicleId}", vehicleId);
                    throw new Exception("Invalid image file");
                }

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "vehicles");
                if (!Directory.Exists(uploadsFolder))
                {
                    _logger.LogInformation("Tworzenie folderu uploads: {UploadsFolder}", uploadsFolder);
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                _logger.LogDebug("Zapisywanie pliku zdjęcia: {FilePath}", filePath);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                var imageUrl = $"/uploads/vehicles/{fileName}";
                vehicle.ImageUrl = imageUrl;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Pomyślnie przesłano zdjęcie dla pojazdu ID: {VehicleId}. " +
                    "URL: '{ImageUrl}', Rozmiar: {FileSize} bajtów",
                    vehicleId, imageUrl, image.Length);

                return imageUrl;
            }
            catch (Exception ex) when (ex.Message == "Invalid image file")
            {
                _logger.LogError(ex, "Błąd walidacji pliku podczas przesyłania zdjęcia dla pojazdu ID: {VehicleId}", vehicleId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas przesyłania zdjęcia dla pojazdu ID: {VehicleId}", vehicleId);
                throw;
            }
        }
    }
}