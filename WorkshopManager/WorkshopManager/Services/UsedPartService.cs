using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkshopManager.Data;
using WorkshopManager.DTOs;
using WorkshopManager.Mappers;
using WorkshopManager.Models;

namespace WorkshopManager.Services
{
    public class UsedPartService : IUsedPartService
    {
        private readonly ApplicationDbContext _context;
        private readonly UsedPartMapper _mapper;
        private readonly ILogger<UsedPartService> _logger;

        public UsedPartService(ApplicationDbContext context, ILogger<UsedPartService> logger)
        {
            _context = context;
            _mapper = new UsedPartMapper();
            _logger = logger;
        }

        public async Task<UsedPartDto> AddPartToTaskAsync(UsedPartCreateDto usedPartDto)
        {
            return await AddPartAsync(usedPartDto);
        }

        public async Task<bool> RemovePartFromTaskAsync(int usedPartId)
        {
            return await RemovePartAsync(usedPartId);
        }

        public async Task<UsedPartDto> AddPartAsync(UsedPartCreateDto usedPartDto)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto dodawanie części ID: {PartId} (ilość: {Quantity})",
                    usedPartDto.PartId, usedPartDto.Quantity);

                var part = await _context.Parts.FindAsync(usedPartDto.PartId);
                if (part == null)
                {
                    _logger.LogWarning("Próba dodania nieistniejącej części ID: {PartId}",
                        usedPartDto.PartId);
                    throw new InvalidOperationException($"Część o ID {usedPartDto.PartId} nie istnieje");
                }

                _logger.LogInformation("Dodawanie części '{PartName}' (cena jednostkowa: {UnitPrice:C})",
                    part.Name, part.UnitPrice);

                var usedPart = _mapper.FromCreateDto(usedPartDto);
                _context.UsedParts.Add(usedPart);
                await _context.SaveChangesAsync();

                usedPart = await _context.UsedParts
                    .Include(up => up.Part)
                    .FirstAsync(up => up.Id == usedPart.Id);

                var result = _mapper.ToDto(usedPart);
                var totalCost = usedPartDto.Quantity * part.UnitPrice;

                _logger.LogInformation("Pomyślnie dodano część '{PartName}'. " +
                    "Ilość: {Quantity}, Koszt jednostkowy: {UnitPrice:C}, Koszt całkowity: {TotalCost:C}",
                    part.Name, usedPartDto.Quantity, part.UnitPrice, totalCost);

                return result;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Błąd bazy danych podczas dodawania części ID: {PartId}",
                    usedPartDto.PartId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas dodawania części ID: {PartId}",
                    usedPartDto.PartId);
                throw;
            }
        }

        public async Task<bool> RemovePartAsync(int usedPartId)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto usuwanie użytej części ID: {UsedPartId}", usedPartId);

                var usedPart = await _context.UsedParts
                    .Include(up => up.Part)
                    .FirstOrDefaultAsync(up => up.Id == usedPartId);

                if (usedPart == null)
                {
                    _logger.LogWarning("Nie znaleziono użytej części o ID: {UsedPartId} do usunięcia", usedPartId);
                    return false;
                }

                var partInfo = $"Część: '{usedPart.Part.Name}', Ilość: {usedPart.Quantity}, " +
                               $"Koszt: {(usedPart.Quantity * usedPart.Part.UnitPrice):C}";

                _context.UsedParts.Remove(usedPart);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Pomyślnie usunięto użytą część ID: {UsedPartId} ({PartInfo})", usedPartId, partInfo);
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Błąd bazy danych podczas usuwania użytej części ID: {UsedPartId}", usedPartId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas usuwania użytej części ID: {UsedPartId}", usedPartId);
                throw;
            }
        }

        public async Task<bool> UpdatePartQuantityAsync(int usedPartId, int newQuantity)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto aktualizację ilości użytej części ID: {UsedPartId} na {NewQuantity}",
                    usedPartId, newQuantity);

                if (newQuantity <= 0)
                {
                    _logger.LogWarning("Próba ustawienia nieprawidłowej ilości {NewQuantity} dla użytej części ID: {UsedPartId}",
                        newQuantity, usedPartId);
                    throw new ArgumentException($"Ilość musi być większa od 0, otrzymano: {newQuantity}");
                }

                var usedPart = await _context.UsedParts
                    .Include(up => up.Part)
                    .FirstOrDefaultAsync(up => up.Id == usedPartId);

                if (usedPart == null)
                {
                    _logger.LogWarning("Nie znaleziono użytej części o ID: {UsedPartId} do aktualizacji ilości", usedPartId);
                    return false;
                }

                var oldQuantity = usedPart.Quantity;
                var oldCost = oldQuantity * usedPart.Part.UnitPrice;
                var newCost = newQuantity * usedPart.Part.UnitPrice;

                usedPart.Quantity = newQuantity;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Zaktualizowano ilość części '{PartName}'. " +
                    "Ilość: {OldQuantity} -> {NewQuantity}, Koszt: {OldCost:C} -> {NewCost:C}",
                    usedPart.Part.Name, oldQuantity, newQuantity, oldCost, newCost);

                return true;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Błąd bazy danych podczas aktualizacji ilości użytej części ID: {UsedPartId}", usedPartId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas aktualizacji ilości użytej części ID: {UsedPartId}", usedPartId);
                throw;
            }
        }

        public async Task<UsedPartDto> GetUsedPartByIdAsync(int usedPartId)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto pobieranie użytej części ID: {UsedPartId}", usedPartId);

                var usedPart = await _context.UsedParts
                    .Include(up => up.Part)
                    .FirstOrDefaultAsync(up => up.Id == usedPartId);

                if (usedPart == null)
                {
                    _logger.LogWarning("Nie znaleziono użytej części o ID: {UsedPartId}", usedPartId);
                    return new UsedPartDto();
                }

                var result = _mapper.ToDto(usedPart);
                var totalCost = usedPart.Quantity * usedPart.Part.UnitPrice;

                _logger.LogInformation("Pomyślnie pobrano użytą część ID: {UsedPartId}. " +
                    "Część: '{PartName}', Ilość: {Quantity}, Koszt: {TotalCost:C}",
                    usedPartId, usedPart.Part.Name, usedPart.Quantity, totalCost);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania użytej części ID: {UsedPartId}", usedPartId);
                throw;
            }
        }
    }
}