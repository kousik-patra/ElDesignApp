using Microsoft.AspNetCore.Identity.UI.Services;
using Resend;
using System.Threading.Tasks;

namespace ElDesignApp.Services;

public class ResendEmailSender : IEmailSender
{
    private readonly ResendClient _resend;
    private readonly IConfiguration _config;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(ResendClient resend, IConfiguration config, ILogger<ResendEmailSender> logger)
    {
        _resend = resend;
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
    {
        var fromEmail = _config["Resend:FromEmail"] ?? "no-reply@yourdomain.com";

        var message = new EmailMessage
        {
            From = fromEmail,
            To = { toEmail },
            Subject = subject,
            HtmlBody = htmlMessage
        };

        try
        {
            await _resend.EmailSendAsync(message);
            _logger.LogInformation("Email sent successfully to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            throw new InvalidOperationException($"Resend failed to send email to {toEmail}: {ex.Message}");
        }
    }
}