using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using WorkshopManager.Services;
using Microsoft.AspNetCore.Authorization;
using WorkshopManager.Extensions; 
using System.Security.Claims;
using WorkshopManager.Models;

namespace WorkshopManager.Controllers
{
    [Authorize(Policy = "WorkshopUser")]
    public class ReportController : Controller
    {
        private readonly IPdfReportService _pdfReportService;
        private readonly ILogger<ReportController> _logger;

        public ReportController(IPdfReportService pdfReportService, ILogger<ReportController> logger)
        {
            _pdfReportService = pdfReportService;
            _logger = logger;
        }

        // GET: /Report/DownloadOpenOrdersReport
        [HttpGet]
        public async Task<IActionResult> DownloadOpenOrdersReport()
        {
            try
            {
                _logger.LogInformation("Użytkownik {UserName} ({UserRole}) żąda pobrania raportu otwartych zleceń",
                    User.Identity?.Name ?? "nieznany",
                    string.Join(", ", User.Claims
                        .Where(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                        .Select(c => c.Value)));

                var pdfBytes = await _pdfReportService.GenerateOpenOrdersReportAsync();

                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    _logger.LogWarning("Wygenerowany raport PDF otwartych zleceń jest pusty dla użytkownika: {UserName}",
                        User.Identity?.Name);
                    TempData["ErrorMessage"] = "Nie udało się wygenerować raportu. Spróbuj ponownie.";
                    return RedirectToAction("Index", "Home");
                }

                _logger.LogInformation("Raport otwartych zleceń wygenerowany pomyślnie dla użytkownika: {UserName}. Rozmiar: {Size} bajtów",
                    User.Identity?.Name, pdfBytes.Length);

                var fileName = $"raport-otwarte-zlecenia-{DateTime.Now:yyyy-MM-dd-HHmm}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas generowania raportu otwartych zleceń dla użytkownika: {UserName}",
                    User.Identity?.Name);
                TempData["ErrorMessage"] = "Wystąpił błąd podczas generowania raportu.";
                return RedirectToAction("Index", "Home");
            }
        }
    }
}