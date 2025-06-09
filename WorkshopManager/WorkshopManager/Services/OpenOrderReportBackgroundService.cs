using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System;
using WorkshopManager.Services;

public class OpenOrderReportBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OpenOrderReportBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(2);
    private int _executionCount = 0;

    public OpenOrderReportBackgroundService(IServiceProvider serviceProvider, ILogger<OpenOrderReportBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("OpenOrderReportBackgroundService uruchamiany. Interwał: {Interval} minut", _interval.TotalMinutes);
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("OpenOrderReportBackgroundService zatrzymywany. Wykonano łącznie {ExecutionCount} cykli", _executionCount);
        return base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OpenOrderReportBackgroundService rozpoczął działanie");

        while (!stoppingToken.IsCancellationRequested)
        {
            var executionId = ++_executionCount;
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInformation("Rozpoczęcie cyklu #{ExecutionId} generowania raportu otwartych zleceń", executionId);

                using var scope = _serviceProvider.CreateScope();

                _logger.LogDebug("Pobieranie serwisów z DI container - cykl #{ExecutionId}", executionId);

                var pdfService = scope.ServiceProvider.GetRequiredService<IPdfReportService>();
                var emailSender = scope.ServiceProvider.GetRequiredService<EmailSenderService>();

                _logger.LogInformation("Generowanie raportu PDF - cykl #{ExecutionId}", executionId);
                var pdfBytes = await pdfService.GenerateOpenOrdersReportAsync();

                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    _logger.LogWarning("Wygenerowany raport PDF jest pusty - cykl #{ExecutionId}", executionId);
                }
                else
                {
                    _logger.LogInformation("Raport PDF wygenerowany pomyślnie. Rozmiar: {PdfSize} bajtów - cykl #{ExecutionId}",
                        pdfBytes.Length, executionId);
                }

                _logger.LogInformation("Wysyłanie raportu emailem - cykl #{ExecutionId}", executionId);
                await emailSender.SendEmailWithAttachmentAsync(
                    subject: "Raport otwartych zleceń",
                    body: "W załączeniu raport z aktualnych otwartych zleceń.",
                    attachmentBytes: pdfBytes,
                    attachmentName: "raport-otwarte-naprawy.pdf");

                var executionTime = DateTime.UtcNow - startTime;
                _logger.LogInformation("Cykl #{ExecutionId} zakończony pomyślnie w czasie {ExecutionTime:hh\\:mm\\:ss}",
                    executionId, executionTime);
            }
            catch (InvalidOperationException ex)
            {
                var executionTime = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "Błąd Dependency Injection podczas cyklu #{ExecutionId} (czas: {ExecutionTime:hh\\:mm\\:ss}). " +
                    "Prawdopodobnie brak zarejestrowanego serwisu", executionId, executionTime);
            }
            catch (Exception ex)
            {
                var executionTime = DateTime.UtcNow - startTime;
                _logger.LogError(ex, "Nieoczekiwany błąd podczas cyklu #{ExecutionId} (czas: {ExecutionTime:hh\\:mm\\:ss}). " +
                    "Serwis będzie kontynuował działanie", executionId, executionTime);
            }

            if (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogDebug("Oczekiwanie {DelayMinutes} minut do następnego cyklu. Następny cykl: #{NextExecutionId}",
                    _interval.TotalMinutes, _executionCount + 1);

                try
                {
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Otrzymano żądanie zatrzymania podczas oczekiwania. Przerywanie pętli");
                    break;
                }
            }
        }

        _logger.LogInformation("OpenOrderReportBackgroundService zakończył działanie po {TotalExecutions} cyklach", _executionCount);
    }
}