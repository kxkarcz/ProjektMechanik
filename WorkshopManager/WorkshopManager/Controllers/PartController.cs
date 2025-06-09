using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkshopManager.Data;
using WorkshopManager.Models;

namespace WorkshopManager.Controllers
{
    [Authorize(Roles = "Admin,Recepcjonista")]
    public class PartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PartController> _logger;

        public PartController(ApplicationDbContext context, ILogger<PartController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Part
        public async Task<IActionResult> Index()
        {
            try
            {
                var parts = await _context.Parts
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                return View(parts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading parts list");
                TempData["ErrorMessage"] = "Wystąpił błąd podczas ładowania listy części.";
                return View(new List<Part>());
            }
        }

        // GET: Part/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var part = await _context.Parts
                    .Include(p => p.UsedParts)
                        .ThenInclude(up => up.ServiceOrder)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (part == null)
                {
                    return NotFound();
                }

                return View(part);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading part details for ID: {PartId}", id);
                TempData["ErrorMessage"] = "Wystąpił błąd podczas ładowania szczegółów części.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Part/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Part/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,UnitPrice,StockQuantity,Description")] Part part)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingPart = await _context.Parts
                        .FirstOrDefaultAsync(p => p.Name.ToLower() == part.Name.ToLower());

                    if (existingPart != null)
                    {
                        ModelState.AddModelError("Name", "Część o tej nazwie już istnieje w magazynie.");
                        return View(part);
                    }

                    _context.Add(part);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Utworzono nową część: {PartName} (ID: {PartId})", part.Name, part.Id);
                    TempData["SuccessMessage"] = $"Część '{part.Name}' została pomyślnie dodana do magazynu.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating new part");
                    TempData["ErrorMessage"] = "Wystąpił błąd podczas dodawania części do magazynu.";
                }
            }

            return View(part);
        }

        // GET: Part/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var part = await _context.Parts.FindAsync(id);
                if (part == null)
                {
                    return NotFound();
                }

                return View(part);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading part for edit. ID: {PartId}", id);
                TempData["ErrorMessage"] = "Wystąpił błąd podczas ładowania danych części.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Part/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,UnitPrice,StockQuantity,Description")] Part part)
        {
            if (id != part.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingPart = await _context.Parts
                        .FirstOrDefaultAsync(p => p.Name.ToLower() == part.Name.ToLower() && p.Id != part.Id);

                    if (existingPart != null)
                    {
                        ModelState.AddModelError("Name", "Część o tej nazwie już istnieje w magazynie.");
                        return View(part);
                    }

                    _context.Update(part);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Zaktualizowano część: {PartName} (ID: {PartId})", part.Name, part.Id);
                    TempData["SuccessMessage"] = $"Część '{part.Name}' została pomyślnie zaktualizowana.";
                    return RedirectToAction(nameof(Details), new { id = part.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PartExists(part.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating part. ID: {PartId}", part.Id);
                    TempData["ErrorMessage"] = "Wystąpił błąd podczas aktualizacji części.";
                }
            }

            return View(part);
        }

        // GET: Part/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var part = await _context.Parts
                    .Include(p => p.UsedParts)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (part == null)
                {
                    return NotFound();
                }

                return View(part);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading part for deletion. ID: {PartId}", id);
                TempData["ErrorMessage"] = "Wystąpił błąd podczas ładowania danych części.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Part/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var part = await _context.Parts
                    .Include(p => p.UsedParts)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (part == null)
                {
                    return NotFound();
                }

                if (part.UsedParts.Any())
                {
                    TempData["ErrorMessage"] = $"Nie można usunąć części '{part.Name}' - jest używana w {part.UsedParts.Count} zleceniach.";
                    return RedirectToAction(nameof(Delete), new { id });
                }

                var partName = part.Name;
                _context.Parts.Remove(part);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Usunięto część: {PartName} (ID: {PartId})", partName, id);
                TempData["SuccessMessage"] = $"Część '{partName}' została pomyślnie usunięta z magazynu.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting part. ID: {PartId}", id);
                TempData["ErrorMessage"] = "Wystąpił błąd podczas usuwania części.";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableParts()
        {
            try
            {
                var parts = await _context.Parts
                    .Where(p => p.StockQuantity > 0)
                    .Select(p => new
                    {
                        id = p.Id,
                        name = p.Name,
                        unitPrice = p.UnitPrice,
                        stockQuantity = p.StockQuantity,
                        description = p.Description
                    })
                    .OrderBy(p => p.name)
                    .ToListAsync();

                return Json(parts, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available parts");
                return Json(new List<object>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStock(int partId, int quantityUsed)
        {
            try
            {
                var part = await _context.Parts.FindAsync(partId);
                if (part == null)
                {
                    return Json(new { success = false, message = "Część nie znaleziona" });
                }

                if (part.StockQuantity < quantityUsed)
                {
                    return Json(new { success = false, message = "Niewystarczająca ilość w magazynie" });
                }

                part.StockQuantity -= quantityUsed;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Zaktualizowano stan magazynowy części {PartName}: -{Quantity}", part.Name, quantityUsed);
                return Json(new { success = true, newStock = part.StockQuantity });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating part stock");
                return Json(new { success = false, message = "Błąd podczas aktualizacji stanu magazynowego" });
            }
        }

        private bool PartExists(int id)
        {
            return _context.Parts.Any(e => e.Id == id);
        }
    }
}