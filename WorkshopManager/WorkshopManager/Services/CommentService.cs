using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkshopManager.Data;
using WorkshopManager.DTOs;
using WorkshopManager.Mappers;
using WorkshopManager.Models;
using System;

namespace WorkshopManager.Services
{
    public class CommentService : ICommentService
    {
        private readonly ApplicationDbContext _context;
        private readonly CommentMapper _commentMapper;
        private readonly ILogger<CommentService> _logger;

        public CommentService(ApplicationDbContext context, ILogger<CommentService> logger)
        {
            _context = context;
            _commentMapper = new CommentMapper();
            _logger = logger;
        }

        public async Task<List<CommentDto>> GetAllAsync()
        {
            try
            {
                _logger.LogInformation("Rozpoczęto pobieranie wszystkich komentarzy");

                var comments = await _context.Comments
                    .Select(c => _commentMapper.ToDto(c))
                    .ToListAsync();

                _logger.LogInformation("Pomyślnie pobrano {Count} komentarzy", comments.Count);
                return comments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania wszystkich komentarzy");
                throw;
            }
        }

        public async Task<List<CommentDto>> GetCommentsByOrderIdAsync(int orderId)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto pobieranie komentarzy dla zamówienia ID: {OrderId}", orderId);

                var comments = await _context.Comments
                    .Where(c => c.ServiceOrderId == orderId)
                    .Select(c => _commentMapper.ToDto(c))
                    .ToListAsync();

                _logger.LogInformation("Pobrano {Count} komentarzy dla zamówienia ID: {OrderId}", comments.Count, orderId);
                return comments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania komentarzy dla zamówienia ID: {OrderId}", orderId);
                throw;
            }
        }

        public async Task<CommentDto> CreateCommentAsync(CommentCreatedDto commentDto)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto tworzenie nowego komentarza przez autora: {Author}", commentDto.Author);

                var comment = _commentMapper.FromDto(commentDto);
                comment.Timestamp = DateTime.UtcNow;

                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                var result = _commentMapper.ToDto(comment);
                _logger.LogInformation("Pomyślnie utworzono komentarz ID: {CommentId} przez autora: {Author}", comment.Id, commentDto.Author);

                return result;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Błąd bazy danych podczas tworzenia komentarza przez autora: {Author}", commentDto.Author);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas tworzenia komentarza przez autora: {Author}", commentDto.Author);
                throw;
            }
        }

        public async Task<CommentDto?> UpdateCommentAsync(int id, CommentCreatedDto updateDto)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto aktualizację komentarza ID: {CommentId}", id);

                var comment = await _context.Comments.FindAsync(id);
                if (comment == null)
                {
                    _logger.LogWarning("Nie znaleziono komentarza o ID: {CommentId} do aktualizacji", id);
                    return null;
                }

                var oldAuthor = comment.Author;
                comment.Content = updateDto.Content;
                comment.Author = updateDto.Author;
                comment.Timestamp = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var result = _commentMapper.ToDto(comment);
                _logger.LogInformation("Pomyślnie zaktualizowano komentarz ID: {CommentId}, autor zmieniony z '{OldAuthor}' na '{NewAuthor}'",
                    id, oldAuthor, updateDto.Author);

                return result;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Błąd bazy danych podczas aktualizacji komentarza ID: {CommentId}", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas aktualizacji komentarza ID: {CommentId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteCommentAsync(int id)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto usuwanie komentarza ID: {CommentId}", id);

                var comment = await _context.Comments.FindAsync(id);
                if (comment == null)
                {
                    _logger.LogWarning("Nie znaleziono komentarza o ID: {CommentId} do usunięcia", id);
                    return false;
                }

                var author = comment.Author;
                _context.Comments.Remove(comment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Pomyślnie usunięto komentarz ID: {CommentId} autora: {Author}", id, author);
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Błąd bazy danych podczas usuwania komentarza ID: {CommentId}", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas usuwania komentarza ID: {CommentId}", id);
                throw;
            }
        }
    }
}