using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WorkshopManager.Services
{
    public class EmailSenderService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPass;
        private readonly string _adminEmail;
        private readonly ILogger<EmailSenderService> _logger;

        public EmailSenderService(IConfiguration configuration, ILogger<EmailSenderService> logger)
        {
            _logger = logger;

            try
            {
                _logger.LogInformation("Inicjalizacja EmailSenderService - odczyt konfiguracji");

                // Odczyt z appsettings.json
                _smtpHost = configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
                _smtpPort = int.Parse(configuration["EmailSettings:SmtpPort"] ?? "587");
                _smtpUser = configuration["EmailSettings:SmtpUser"] ?? throw new ArgumentNullException("SmtpUser not configured");
                _smtpPass = configuration["EmailSettings:SmtpPass"] ?? throw new ArgumentNullException("SmtpPass not configured");
                _adminEmail = configuration["EmailSettings:AdminEmail"] ?? _smtpUser;

                _logger.LogInformation("EmailSenderService skonfigurowany pomyślnie. Host: {SmtpHost}, Port: {SmtpPort}, User: {SmtpUser}, AdminEmail: {AdminEmail}",
                    _smtpHost, _smtpPort, _smtpUser, _adminEmail);
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogError(ex, "Błąd konfiguracji EmailSenderService - brak wymaganego parametru: {ParameterName}", ex.ParamName);
                throw;
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Błąd konfiguracji EmailSenderService - nieprawidłowy format portu SMTP");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas inicjalizacji EmailSenderService");
                throw;
            }
        }

        public async Task SendEmailWithAttachmentAsync(string subject, string body, byte[] attachmentBytes, string attachmentName)
        {
            try
            {
                _logger.LogInformation("Rozpoczęto wysyłanie emaila. Temat: '{Subject}', Odbiorca: {AdminEmail}, Załącznik: '{AttachmentName}' ({AttachmentSize} bajtów)",
                    subject, _adminEmail, attachmentName ?? "brak", attachmentBytes?.Length ?? 0);

                using var message = new MailMessage
                {
                    From = new MailAddress(_smtpUser),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false
                };

                message.To.Add(_adminEmail);

                if (attachmentBytes != null && attachmentBytes.Length > 0)
                {
                    _logger.LogInformation("Dodawanie załącznika: '{AttachmentName}' o rozmiarze {AttachmentSize} bajtów",
                        attachmentName, attachmentBytes.Length);

                    var attachment = new Attachment(new MemoryStream(attachmentBytes), attachmentName);
                    message.Attachments.Add(attachment);
                }
                else
                {
                    _logger.LogInformation("Email bez załącznika");
                }

                using var client = new SmtpClient(_smtpHost, _smtpPort)
                {
                    Credentials = new NetworkCredential(_smtpUser, _smtpPass),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Timeout = 30000
                };

                _logger.LogInformation("Nawiązywanie połączenia z serwerem SMTP: {SmtpHost}:{SmtpPort}", _smtpHost, _smtpPort);

                await client.SendMailAsync(message);

                _logger.LogInformation("Email wysłany pomyślnie. Temat: '{Subject}', Odbiorca: {AdminEmail}", subject, _adminEmail);
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "Błąd SMTP podczas wysyłania emaila. Temat: '{Subject}', Kod błędu: {StatusCode}, Szczegóły: {SmtpDetails}",
                    subject, ex.StatusCode, ex.Message);
                throw;
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Błąd argumentów podczas wysyłania emaila. Temat: '{Subject}', Parametr: {ParamName}",
                    subject, ex.ParamName);
                throw;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Błąd operacji podczas wysyłania emaila. Temat: '{Subject}', Szczegóły: {Details}",
                    subject, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd podczas wysyłania emaila. Temat: '{Subject}', Odbiorca: {AdminEmail}",
                    subject, _adminEmail);
                throw;
            }
        }
    }
}