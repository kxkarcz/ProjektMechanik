using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using WorkshopManager.Data;

namespace WorkshopManager.Services
{
    public class PdfReportService : IPdfReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PdfReportService> _logger;

        public PdfReportService(ApplicationDbContext context, ILogger<PdfReportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<byte[]> GenerateOpenOrdersReportAsync()
        {
            try
            {
                _logger.LogInformation("Rozpoczęto generowanie raportu otwartych zleceń");

                QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

                _logger.LogDebug("Pobieranie danych otwartych zleceń z bazy danych");
                var openOrders = await _context.ServiceOrders
                    .Include(o => o.Vehicle)
                        .ThenInclude(v => v.Customer)
                    .Where(o => o.Status == "Open")
                    .ToListAsync();

                _logger.LogInformation("Pobrano {Count} otwartych zleceń do raportu", openOrders.Count);

                if (!openOrders.Any())
                {
                    _logger.LogWarning("Brak otwartych zleceń do wygenerowania raportu");
                }

                _logger.LogDebug("Rozpoczęcie generowania dokumentu PDF");
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(50);
                        page.Size(PageSizes.A4);
                        page.Header().Text("Raport otwartych zleceń").Bold().FontSize(20);

                        page.Content().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Klient");
                                header.Cell().Text("Pojazd");
                                header.Cell().Text("Status");
                                header.Cell().Text("ID");
                            });

                            foreach (var order in openOrders)
                            {
                                table.Cell().Text($"{order.Vehicle.Customer.FirstName} {order.Vehicle.Customer.LastName}");
                                table.Cell().Text(order.Vehicle.RegistrationNumber);
                                table.Cell().Text(order.Status);
                                table.Cell().Text(order.Id.ToString());
                            }
                        });

                        page.Footer()
                            .AlignCenter()
                            .Text(txt =>
                            {
                                txt.Span("Strona ");
                                txt.CurrentPageNumber();
                                txt.Span(" / ");
                                txt.TotalPages();
                            });
                    });
                });

                _logger.LogDebug("Konwersja dokumentu do formatu PDF");
                using var stream = new MemoryStream();
                document.GeneratePdf(stream);
                var pdfBytes = stream.ToArray();

                _logger.LogInformation("Raport otwartych zleceń wygenerowany pomyślnie. " +
                    "Liczba zleceń: {OrderCount}, Rozmiar PDF: {PdfSize} bajtów",
                    openOrders.Count, pdfBytes.Length);

                return pdfBytes;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Błąd operacji podczas generowania raportu otwartych zleceń. " +
                    "Możliwy problem z konfiguracją QuestPDF lub danymi");
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Błąd bazy danych podczas pobierania danych do raportu otwartych zleceń");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas generowania raportu otwartych zleceń");
                throw;
            }
        }

        public async Task<byte[]> GenerateServiceOrderReportAsync(int orderId)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto generowanie raportu dla zlecenia ID: {OrderId}", orderId);

                // pobranie zlecenia z zadaniami, użytymi częściami i komentarzami
                _logger.LogDebug("Pobieranie szczegółowych danych zlecenia ID: {OrderId} z bazy danych", orderId);
                var order = await _context.ServiceOrders
                    .Include(o => o.Vehicle)
                    .ThenInclude(v => v.Customer)
                    .Include(o => o.ServiceTasks)
                        .ThenInclude(t => t.UsedParts)
                            .ThenInclude(up => up.Part)
                    .Include(o => o.Comments)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    _logger.LogWarning("Nie znaleziono zlecenia o ID: {OrderId} do wygenerowania raportu", orderId);
                    return null!;
                }

                _logger.LogInformation("Pobrano zlecenie ID: {OrderId} dla klienta: {CustomerName}, " +
                    "pojazd: {VehicleRegistration}, zadań: {TaskCount}, komentarzy: {CommentCount}",
                    orderId,
                    $"{order.Vehicle.Customer.FirstName} {order.Vehicle.Customer.LastName}",
                    order.Vehicle.RegistrationNumber,
                    order.ServiceTasks.Count,
                    order.Comments.Count);

                // całkowity koszt: sumy kosztów pracy i części
                var laborTotal = order.ServiceTasks.Sum(t => t.LaborCost);
                var partsTotal = order.ServiceTasks
                    .SelectMany(t => t.UsedParts)
                    .Sum(up => up.Quantity * up.Part.UnitPrice);
                var grandTotal = laborTotal + partsTotal;

                var totalParts = order.ServiceTasks.SelectMany(t => t.UsedParts).Sum(up => up.Quantity);

                _logger.LogInformation("Kalkulacja kosztów zlecenia ID: {OrderId}: " +
                    "Robocizna: {LaborTotal:C}, Części: {PartsTotal:C} ({TotalPartsCount} szt.), " +
                    "Suma: {GrandTotal:C}",
                    orderId, laborTotal, partsTotal, totalParts, grandTotal);

                _logger.LogDebug("Rozpoczęcie generowania dokumentu PDF dla zlecenia ID: {OrderId}", orderId);

                // generowanie dokumentu
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(50);
                        page.Size(PageSizes.A4);
                        page.Header()
                            .Text($"Raport zlecenia serwisowego #{order.Id}")
                            .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);
                        page.Content()
                            .PaddingVertical(10)
                            .Column(col =>
                            {
                                // Dane klienta i pojazdu
                                col.Item().Text($"Klient: {order.Vehicle.Customer.FirstName} {order.Vehicle.Customer.LastName}");
                                col.Item().Text($"Email: {order.Vehicle.Customer.Email}, Tel.: {order.Vehicle.Customer.PhoneNumber}");
                                col.Item().Text($"Pojazd: {order.Vehicle.RegistrationNumber} (VIN: {order.Vehicle.VIN})");
                                col.Item().Text($"Status: {order.Status}");
                                col.Item().Text($"Mechanik: {order.AssignedMechanicId}");
                                col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                                // Tabela zadań
                                col.Item().Text("Zadania serwisowe:").Bold();
                                col.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.ConstantColumn(80);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Text("Opis zadania");
                                        header.Cell().Text("Koszt pracy");
                                    });

                                    foreach (var task in order.ServiceTasks)
                                    {
                                        table.Cell().Text(task.Description);
                                        table.Cell().Text($"{task.LaborCost:C}");
                                    }
                                });

                                col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                                // części
                                col.Item().Text("Użyte części:").Bold();
                                col.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(200);
                                        columns.ConstantColumn(60);
                                        columns.ConstantColumn(80);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Text("Nazwa części");
                                        header.Cell().Text("Ilość");
                                        header.Cell().Text("Cena");
                                    });

                                    foreach (var up in order.ServiceTasks.SelectMany(t => t.UsedParts))
                                    {
                                        table.Cell().Text(up.Part.Name);
                                        table.Cell().Text(up.Quantity.ToString());
                                        table.Cell().Text($"{(up.Quantity * up.Part.UnitPrice):C}");
                                    }
                                });

                                col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                                // komentarze
                                col.Item().Text("Komentarze:").Bold();
                                foreach (var cm in order.Comments)
                                {
                                    col.Item().Text($"{cm.Timestamp:yyyy-MM-dd HH:mm} • {cm.Author}: {cm.Content}");
                                }

                                col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                                // suma
                                col.Item().AlignRight().Text($"Razem (robocizna): {laborTotal:C}").Bold();
                                col.Item().AlignRight().Text($"Razem (części): {partsTotal:C}").Bold();
                                col.Item().AlignRight().Text($"Do zapłaty: {grandTotal:C}").SemiBold();
                            });
                        page.Footer()
                            .AlignCenter()
                            .Text(txt =>
                            {
                                txt.Span("Strona ");
                                txt.CurrentPageNumber();
                                txt.Span(" / ");
                                txt.TotalPages();
                            });

                    });
                });

                _logger.LogDebug("Konwersja dokumentu do formatu PDF dla zlecenia ID: {OrderId}", orderId);
                using var stream = new MemoryStream();
                document.GeneratePdf(stream);
                var pdfBytes = stream.ToArray();

                _logger.LogInformation("Raport zlecenia ID: {OrderId} wygenerowany pomyślnie. " +
                    "Rozmiar PDF: {PdfSize} bajtów, Wartość zlecenia: {GrandTotal:C}",
                    orderId, pdfBytes.Length, grandTotal);

                return pdfBytes;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Błąd operacji podczas generowania raportu zlecenia ID: {OrderId}. " +
                    "Możliwy problem z konfiguracją QuestPDF lub strukturą danych", orderId);
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Błąd bazy danych podczas pobierania danych zlecenia ID: {OrderId}", orderId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas generowania raportu zlecenia ID: {OrderId}", orderId);
                throw;
            }
        }
    }
}