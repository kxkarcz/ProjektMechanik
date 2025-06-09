using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkshopManager.Data;
using WorkshopManager.DTOs;
using WorkshopManager.Mappers;
using WorkshopManager.Models;

namespace WorkshopManager.Services
{
    public class PartService : IPartService
    {
        private readonly ApplicationDbContext _context;
        private readonly PartMapper _mapper;
        private readonly ILogger<PartService> _logger;

        public PartService(ApplicationDbContext context, ILogger<PartService> logger)
        {
            _context = context;
            _mapper = new PartMapper();
            _logger = logger;
        }

        public async Task<List<PartDto>> GetAllPartsAsync()
        {
            try
            {
                _logger.LogInformation("Rozpoczęto pobieranie wszystkich części");

                var parts = await _context.Parts.ToListAsync();
                var result = parts.Select(_mapper.ToDto).ToList();

                _logger.LogInformation("Pomyślnie pobrano {Count} części", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania wszystkich części");
                throw;
            }
        }

        public async Task<PartDto?> GetPartByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto pobieranie części ID: {PartId}", id);

                var part = await _context.Parts.FindAsync(id);

                if (part == null)
                {
                    _logger.LogWarning("Nie znaleziono części o ID: {PartId}", id);
                    return null;
                }

                var result = _mapper.ToDto(part);
                _logger.LogInformation("Pomyślnie pobrano część ID: {PartId}, Nazwa: '{PartName}'", id, part.Name);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania części ID: {PartId}", id);
                throw;
            }
        }

        public async Task<List<PartDto>> SearchPartsAsync(string searchTerm)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto wyszukiwanie części z frazą: '{SearchTerm}'", searchTerm);

                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    _logger.LogWarning("Pusty termin wyszukiwania - zwracanie pustej listy");
                    return new List<PartDto>();
                }

                var parts = await _context.Parts
                    .Where(p => p.Name.Contains(searchTerm))
                    .ToListAsync();

                var result = parts.Select(_mapper.ToDto).ToList();

                _logger.LogInformation("Wyszukiwanie zakończone. Znaleziono {Count} części dla frazy: '{SearchTerm}'",
                    result.Count, searchTerm);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas wyszukiwania części z frazą: '{SearchTerm}'", searchTerm);
                throw;
            }
        }

        public async Task<PartDto> CreatePartAsync(PartCreateDto partDto)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto tworzenie nowej części: '{PartName}'", partDto.Name);

                var part = _mapper.FromCreateDto(partDto);
                _context.Parts.Add(part);
                await _context.SaveChangesAsync();

                var result = _mapper.ToDto(part);
                _logger.LogInformation("Pomyślnie utworzono część ID: {PartId}, Nazwa: '{PartName}'",
                    part.Id, partDto.Name);

                return result;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Błąd bazy danych podczas tworzenia części: '{PartName}'", partDto.Name);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas tworzenia części: '{PartName}'", partDto.Name);
                throw;
            }
        }

        public async Task<PartDto?> UpdatePartAsync(int id, PartUpdateDto partDto)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto aktualizację części ID: {PartId}", id);

                var part = await _context.Parts.FindAsync(id);
                if (part == null)
                {
                    _logger.LogWarning("Nie znaleziono części o ID: {PartId} do aktualizacji", id);
                    return null;
                }

                var oldName = part.Name;
                _mapper.UpdateEntity(partDto, part);
                await _context.SaveChangesAsync();

                var result = _mapper.ToDto(part);
                _logger.LogInformation("Pomyślnie zaktualizowano część ID: {PartId}. Nazwa zmieniona z '{OldName}' na '{NewName}'",
                    id, oldName, part.Name);

                return result;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Błąd bazy danych podczas aktualizacji części ID: {PartId}", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas aktualizacji części ID: {PartId}", id);
                throw;
            }
        }

        public async Task<bool> DeletePartAsync(int id)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto usuwanie części ID: {PartId}", id);

                var part = await _context.Parts.FindAsync(id);
                if (part == null)
                {
                    _logger.LogWarning("Nie znaleziono części o ID: {PartId} do usunięcia", id);
                    return false;
                }

                var partName = part.Name;
                _context.Parts.Remove(part);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Pomyślnie usunięto część ID: {PartId}, Nazwa: '{PartName}'", id, partName);
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Błąd bazy danych podczas usuwania części ID: {PartId}. " +
                    "Możliwe że część jest używana w innych rekordach", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas usuwania części ID: {PartId}", id);
                throw;
            }
        }
    }
}