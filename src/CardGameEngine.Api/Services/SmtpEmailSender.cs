using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace CardGameEngine.Api.Services;

/// <summary>
/// Sends real mail via SMTP when Email:Host is configured. Falls back to logging the message
/// (subject + body, never the raw request) so `dotnet run` works with zero mail setup in dev —
/// nothing about registration/confirmation requires SMTP to be configured to run locally.
/// </summary>
public class SmtpEmailSender : IAppEmailSender
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        var host = _config["Email:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogInformation(
                "Email:Host not configured — logging instead of sending. To: {To}, Subject: {Subject}\n{Body}",
                toEmail, subject, htmlBody);
            return;
        }

        var port = int.TryParse(_config["Email:Port"], out var p) ? p : 465;
        var user = _config["Email:User"] ?? "";
        var password = _config["Email:Password"] ?? "";
        var from = _config["Email:From"] ?? user;

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, SecureSocketOptions.SslOnConnect);
        if (!string.IsNullOrEmpty(user))
            await client.AuthenticateAsync(user, password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
