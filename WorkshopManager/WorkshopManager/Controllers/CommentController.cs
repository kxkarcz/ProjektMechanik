using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkshopManager.DTOs;
using WorkshopManager.Services;
using WorkshopManager.Extensions;
using System.Security.Claims;
using WorkshopManager.Models;

namespace WorkshopManager.Controllers
{
    [Authorize(Policy = "WorkshopUser")]
    public class CommentController : Controller
    {
        private readonly ICommentService _commentService;
        private readonly ILogger<CommentController> _logger;

        public CommentController(ICommentService commentService, ILogger<CommentController> logger)
        {
            _commentService = commentService;
            _logger = logger;
        }

        // GET: /Comments
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Rozpoczęcie pobierania listy wszystkich komentarzy");

            try
            {
                var comments = await _commentService.GetAllAsync();
                _logger.LogInformation("Pobrano {Count} komentarzy", comments.Count());
                return View(comments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania listy komentarzy");
                TempData["ErrorMessage"] = "Wystąpił błąd podczas pobierania komentarzy.";
                return View(new List<CommentDto>());
            }
        }

        // GET: /Comments/Details/5
        public async Task<IActionResult> Details(int id)
        {
            _logger.LogInformation("Rozpoczęcie pobierania szczegółów komentarza o ID: {CommentId}", id);

            try
            {
                var comments = await _commentService.GetAllAsync();
                var comment = comments.FirstOrDefault(c => c.Id == id);

                if (comment == null)
                {
                    _logger.LogWarning("Nie znaleziono komentarza o ID: {CommentId}", id);
                    return NotFound();
                }

                _logger.LogInformation("Pobrano szczegóły komentarza o ID: {CommentId}", id);
                return View(comment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania szczegółów komentarza o ID: {CommentId}", id);
                return NotFound();
            }
        }

        // GET: /Comments/Create
        public IActionResult Create(int? orderId = null)
        {
            _logger.LogInformation("Wyświetlanie formularza tworzenia komentarza dla zlecenia: {OrderId}", orderId ?? 0);

            var dto = new CommentCreatedDto();
            if (orderId.HasValue)
                dto.ServiceOrderId = orderId.Value;

            return View(dto);
        }

        // POST: /Comments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CommentCreatedDto dto)
        {
            _logger.LogInformation("Rozpoczęcie tworzenia komentarza dla zlecenia: {OrderId} przez użytkownika: {Author}",
                dto.ServiceOrderId, dto.Author);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model komentarza nie jest prawidłowy dla zlecenia: {OrderId}", dto.ServiceOrderId);
                return View(dto);
            }

            try
            {
                await _commentService.CreateCommentAsync(dto);
                _logger.LogInformation("Pomyślnie utworzono komentarz dla zlecenia: {OrderId} przez użytkownika: {Author}",
                    dto.ServiceOrderId, dto.Author);

                TempData["SuccessMessage"] = "Komentarz został pomyślnie dodany.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas tworzenia komentarza dla zlecenia: {OrderId} przez użytkownika: {Author}",
                    dto.ServiceOrderId, dto.Author);

                ModelState.AddModelError("", "Wystąpił błąd podczas dodawania komentarza.");
                return View(dto);
            }
        }

        // GET: /Comments/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            _logger.LogInformation("Wyświetlanie formularza edycji komentarza o ID: {CommentId}", id);

            try
            {
                var comments = await _commentService.GetAllAsync();
                var comment = comments.FirstOrDefault(c => c.Id == id);

                if (comment == null)
                {
                    _logger.LogWarning("Nie znaleziono komentarza o ID: {CommentId} do edycji", id);
                    return NotFound();
                }

                var dto = new CommentCreatedDto
                {
                    Author = comment.Author,
                    Content = comment.Content,
                    ServiceOrderId = comment.ServiceOrderId
                };

                ViewBag.CommentId = id;
                _logger.LogInformation("Załadowano dane komentarza o ID: {CommentId} do edycji", id);
                return View(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas ładowania komentarza o ID: {CommentId} do edycji", id);
                return NotFound();
            }
        }

        // POST: /Comments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CommentCreatedDto dto)
        {
            _logger.LogInformation("Rozpoczęcie aktualizacji komentarza o ID: {CommentId} przez użytkownika: {Author}",
                id, dto.Author);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model komentarza nie jest prawidłowy podczas edycji komentarza o ID: {CommentId}", id);
                ViewBag.CommentId = id;
                return View(dto);
            }

            try
            {
                var updated = await _commentService.UpdateCommentAsync(id, dto);
                if (updated == null)
                {
                    _logger.LogWarning("Nie znaleziono komentarza o ID: {CommentId} do aktualizacji", id);
                    return NotFound();
                }

                _logger.LogInformation("Pomyślnie zaktualizowano komentarz o ID: {CommentId} przez użytkownika: {Author}",
                    id, dto.Author);

                TempData["SuccessMessage"] = "Komentarz został pomyślnie zaktualizowany.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas aktualizacji komentarza o ID: {CommentId} przez użytkownika: {Author}",
                    id, dto.Author);

                ModelState.AddModelError("", "Wystąpił błąd podczas aktualizacji komentarza.");
                ViewBag.CommentId = id;
                return View(dto);
            }
        }

        // GET: /Comments/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("Wyświetlanie potwierdzenia usunięcia komentarza o ID: {CommentId}", id);

            try
            {
                var comment = await _commentService.GetCommentsByOrderIdAsync(id);

                if (comment == null)
                {
                    _logger.LogWarning("Nie znaleziono komentarza o ID: {CommentId} do usunięcia", id);
                    return NotFound();
                }

                _logger.LogInformation("Załadowano komentarz o ID: {CommentId} do potwierdzenia usunięcia", id);
                return View(comment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas ładowania komentarza o ID: {CommentId} do usunięcia", id);
                return NotFound();
            }
        }

        // POST: /Comments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            _logger.LogInformation("Rozpoczęcie usuwania komentarza o ID: {CommentId}", id);

            try
            {
                var result = await _commentService.DeleteCommentAsync(id);
                if (!result)
                {
                    _logger.LogWarning("Nie znaleziono komentarza o ID: {CommentId} do usunięcia", id);
                    return NotFound();
                }

                _logger.LogInformation("Pomyślnie usunięto komentarz o ID: {CommentId}", id);
                TempData["SuccessMessage"] = "Komentarz został pomyślnie usunięty.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas usuwania komentarza o ID: {CommentId}", id);
                TempData["ErrorMessage"] = "Wystąpił błąd podczas usuwania komentarza.";
                return RedirectToAction(nameof(Index));
            }
        }

    }

}