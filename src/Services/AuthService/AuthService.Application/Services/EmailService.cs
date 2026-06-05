namespace AuthService.Application.Services;

using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Results;

public sealed class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string? _smtpUsername;
    private readonly string? _smtpPassword;
    private readonly bool _smtpEnableSsl;
    private readonly string _fromAddress;
    private readonly string _fromName;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _logger = logger;

        _smtpHost = configuration["AuthEmail:SmtpHost"] ?? "localhost";
        _smtpPort = int.TryParse(configuration["AuthEmail:SmtpPort"], out var port) ? port : 1025;
        _smtpUsername = configuration["AuthEmail:SmtpUsername"];
        _smtpPassword = configuration["AuthEmail:SmtpPassword"];
        _smtpEnableSsl = bool.TryParse(configuration["AuthEmail:EnableSsl"], out var enableSsl) && enableSsl;
        _fromAddress = configuration["AuthEmail:FromAddress"] ?? "no-reply@photoapp.local";
        _fromName = configuration["AuthEmail:FromName"] ?? "PhotoApp";
    }

    public Task<Result> SendEmailVerificationAsync(string email, string verificationLink, CancellationToken cancellationToken = default)
    {
        return SendVerificationEmailAsync(email, verificationLink, cancellationToken);
    }

    public Task<Result> SendTwoFactorCodeAsync(string email, string code, CancellationToken cancellationToken = default)
    {
        return SendCodeEmailAsync(email, code, cancellationToken);
    }

    public Task<Result> SendPasswordResetAsync(string email, string resetLink, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> SendLoginAlertAsync(string email, string ipAddress, string userAgent, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success());
    }

    private async Task<Result> SendVerificationEmailAsync(string email, string verificationLink, CancellationToken cancellationToken)
    {
        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_fromAddress, _fromName),
                Subject = "Potwierdź adres e-mail",
                Body = BuildVerificationBody(email, verificationLink),
                IsBodyHtml = true,
                BodyEncoding = System.Text.Encoding.UTF8
            };

            message.To.Add(email);

            using var client = new SmtpClient(_smtpHost, _smtpPort)
            {
                EnableSsl = _smtpEnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            if (!string.IsNullOrWhiteSpace(_smtpUsername))
            {
                client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
            }

            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Verification email sent to {Email}.", email);
            return Result.Success();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to send verification email to {Email}.", email);
            return Result.Failure(Error.InternalError("Unable to send verification email."));
        }
    }

    private async Task<Result> SendCodeEmailAsync(string email, string code, CancellationToken cancellationToken)
    {
        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_fromAddress, _fromName),
                Subject = "Twój kod logowania",
                Body = BuildTwoFactorBody(email, code),
                IsBodyHtml = true,
                BodyEncoding = System.Text.Encoding.UTF8
            };

            message.To.Add(email);

            using var client = new SmtpClient(_smtpHost, _smtpPort)
            {
                EnableSsl = _smtpEnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            if (!string.IsNullOrWhiteSpace(_smtpUsername))
            {
                client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
            }

            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Two-factor code sent to {Email}.", email);
            return Result.Success();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to send two-factor code to {Email}.", email);
            return Result.Failure(Error.InternalError("Unable to send verification code."));
        }
    }

    private static string BuildVerificationBody(string email, string verificationLink)
    {
        return $"""
               <html>
                 <body style="font-family: Arial, sans-serif; color: #1f2937; line-height: 1.5;">
                   <h2 style="margin-bottom: 0.5rem;">Potwierdź adres e-mail</h2>
                   <p>Cześć {System.Net.WebUtility.HtmlEncode(email)},</p>
                   <p>Utworzono konto w PhotoApp. Aby je aktywować, kliknij poniższy link:</p>
                   <p><a href="{System.Net.WebUtility.HtmlEncode(verificationLink)}">Potwierdź adres e-mail</a></p>
                   <p>Jeśli nie zakładałeś konta, możesz zignorować tę wiadomość.</p>
                 </body>
               </html>
               """;
    }

        private static string BuildTwoFactorBody(string email, string code)
        {
                return $"""
                             <html>
                                 <body style="font-family: Arial, sans-serif; color: #1f2937; line-height: 1.5;">
                                     <h2 style="margin-bottom: 0.5rem;">Kod logowania</h2>
                                     <p>Cześć {System.Net.WebUtility.HtmlEncode(email)},</p>
                                     <p>Twój jednorazowy kod logowania to:</p>
                                     <p style="font-size: 1.8rem; font-weight: 700; letter-spacing: 0.2rem;">{System.Net.WebUtility.HtmlEncode(code)}</p>
                                     <p>Kod wygasa po kilku minutach. Jeśli to nie Ty próbowałeś się zalogować, zignoruj tę wiadomość.</p>
                                 </body>
                             </html>
                             """;
        }
}