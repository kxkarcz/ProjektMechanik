using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkshopManager.Data;
using WorkshopManager.Models;
using WorkshopManager.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

namespace WorkshopManager.Controllers
{
    [Authorize(Roles = "Admin,Recepcjonista,Mechanik")]
    public class UsedPartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UsedPartController> _logger;

        public UsedPartController(ApplicationDbContext context, ILogger<UsedPartController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: UsedPart/Create
        public IActionResult Create(int serviceOrderId)
        {
            try
            {
                ViewBag.ServiceOrderId = serviceOrderId;

                var partsData = _context.Parts
                    .Where(p => p.StockQuantity > 0)
                    .OrderBy(p => p.Name)
                    .Select(p => new
                    {
                        Id = p.Id,
                        Name = p.Name,
                        UnitPrice = p.UnitPrice,
                        StockQuantity = p.StockQuantity,
                        Description = p.Description
                    })
                    .ToList();

                ViewBag.Parts = partsData;

                ViewBag.PartsJson = System.Text.Json.JsonSerializer.Serialize(partsData, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading parts for order {OrderId}", serviceOrderId);
                TempData["ErrorMessage"] = "Wystąpił błąd podczas ładowania części.";
                return RedirectToAction("Details", "ServiceOrder", new { id = serviceOrderId });
            }
        }

        // POST: UsedPart/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PartId,Quantity,ServiceOrderId")] UsedPart usedPart)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (ModelState.IsValid)
                {
                    // 1. Pobierz część i sprawdź dostępność
                    var part = await _context.Parts.FindAsync(usedPart.PartId);
                    if (part == null)
                    {
                        ModelState.AddModelError("PartId", "Nie znaleziono części");
                        throw new Exception("Part not found");
                    }

                    if (part.StockQuantity < usedPart.Quantity)
                    {
                        ModelState.AddModelError("Quantity", $"Niewystarczająca ilość. Dostępne: {part.StockQuantity}");
                        throw new Exception("Insufficient stock");
                    }

                    // 2. Zmniejsz stan magazynowy
                    part.StockQuantity -= usedPart.Quantity;
                    _context.Update(part);

                    // 3. Dodaj użycie części
                    _context.Add(usedPart);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "Dodano część do zlecenia";
                    return RedirectToAction("Details", "ServiceOrder", new { id = usedPart.ServiceOrderId });
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding part to order {OrderId}", usedPart.ServiceOrderId);

                if (!ModelState.IsValid)
                {
                    ViewBag.ServiceOrderId = usedPart.ServiceOrderId;
                    ViewBag.Parts = _context.Parts
                        .Where(p => p.StockQuantity > 0)
                        .OrderBy(p => p.Name)
                        .ToList();
                    return View(usedPart);
                }

                TempData["ErrorMessage"] = "Wystąpił błąd podczas dodawania części.";
            }

            return RedirectToAction("Details", "ServiceOrder", new { id = usedPart.ServiceOrderId });
        }

        // GET: UsedPart/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var usedPart = await _context.UsedParts
                .Include(up => up.Part)
                .FirstOrDefaultAsync(up => up.Id == id);

            if (usedPart == null) return NotFound();

            var dto = new UsedPartDto
            {
                Id = usedPart.Id,
                PartId = usedPart.PartId,
                Quantity = usedPart.Quantity,
                ServiceOrderId = usedPart.ServiceOrderId ?? 0,
                PartName = usedPart.Part?.Name ?? "Brak danych",
                PartPrice = usedPart.Part?.UnitPrice ?? 0,
                PartStockQuantity = usedPart.Part?.StockQuantity ?? 0
            };

            return View(dto);
        }


        // POST: UsedPart/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UsedPartDto model)
        {
            if (id != model.Id)
                return NotFound();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (ModelState.IsValid)
                {
                    var usedPart = await _context.UsedParts
                        .Include(up => up.Part)
                        .FirstOrDefaultAsync(up => up.Id == id);

                    if (usedPart == null) return NotFound();

                    var part = await _context.Parts.FindAsync(usedPart.PartId);
                    if (part != null)
                    {
                        part.StockQuantity += usedPart.Quantity;
                    }

                    if (part.StockQuantity < model.Quantity)
                    {
                        ModelState.AddModelError("Quantity", $"Niewystarczająca ilość. Dostępne: {part.StockQuantity}");
                        throw new Exception("Insufficient stock");
                    }

                    part.StockQuantity -= model.Quantity;

                    usedPart.Quantity = model.Quantity;

                    _context.Update(part);
                    _context.Update(usedPart);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "Zaktualizowano użycie części";
                    return RedirectToAction("Details", "ServiceOrder", new { id = model.ServiceOrderId });
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating used part {UsedPartId}", id);
                TempData["ErrorMessage"] = "Wystąpił błąd podczas aktualizacji części.";
            }

            return View(model);
        }

        public async Task<IActionResult> Index()
        {
            var usedParts = await _context.UsedParts
                .Include(up => up.Part)
                .Include(up => up.ServiceOrder)
                .ToListAsync();

            return View(usedParts);
        }
        [HttpGet]
        public IActionResult GetUsedPartsForOrder(int orderId)
        {
            var usedParts = _context.UsedParts
                .Where(up => up.ServiceOrderId == orderId)
                .Select(up => new {
                    id = up.Id,
                    quantity = up.Quantity,
                    part = new
                    {
                        id = up.Part.Id,
                        name = up.Part.Name,
                        unitPrice = up.Part.UnitPrice
                    }
                })
                .ToList();

            return Json(usedParts);
        }

        // POST: UsedPart/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var usedPart = await _context.UsedParts
                    .Include(up => up.Part)
                    .FirstOrDefaultAsync(up => up.Id == id);

                if (usedPart == null)
                {
                    return NotFound();
                }

                // 1. Przywróć stan magazynowy
                var part = usedPart.Part;
                if (part != null)
                {
                    part.StockQuantity += usedPart.Quantity;
                    _context.Update(part);
                }

                // 2. Usuń użycie części
                _context.UsedParts.Remove(usedPart);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Usunięto część z zlecenia";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting used part {UsedPartId}", id);
                TempData["ErrorMessage"] = "Wystąpił błąd podczas usuwania części.";
            }

            return RedirectToAction("Details", "ServiceOrder", new { id = Request.Form["serviceOrderId"] });
        }
    }
}