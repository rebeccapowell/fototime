using System.Net;
using System.Net.Mail;
using System.Text;
using FotoTime.Application.Invitations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Invitations;

public sealed class MailOptions
{
    public const string SectionName = "Mail";

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 1025;

    public bool EnableSsl { get; set; }

    public string FromAddress { get; set; } = "no-reply@fototime.local";

    public string? FromDisplayName { get; set; } = "FotoTime";

    public string? Username { get; set; }

    public string? Password { get; set; }
}

public sealed class InviteEmailService : IInviteEmailService
{
    private readonly MailOptions _options;
    private readonly ILogger<InviteEmailService> _logger;

    public InviteEmailService(IOptions<MailOptions> options, ILogger<InviteEmailService> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(_options.FromAddress))
        {
            throw new InvalidOperationException("Mail options must specify a from address.");
        }
    }

    public Task SendInviteAsync(InviteEmailContext context, CancellationToken cancellationToken = default)
        => SendAsync(context, "You're invited to FotoTime", BuildInviteBody(context), cancellationToken);

    public Task SendReminderAsync(InviteEmailContext context, CancellationToken cancellationToken = default)
        => SendAsync(context, "Reminder: Your FotoTime invite is waiting", BuildReminderBody(context), cancellationToken);

    private async Task SendAsync(InviteEmailContext context, string subject, string body, CancellationToken cancellationToken)
    {
        using var client = CreateClient();
        using var message = CreateMessage(context.Email, subject, body);

        try
        {
            await client.SendMailAsync(message, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation(
                "Invite email '{Subject}' sent for invite {InviteId}",
                subject,
                context.InviteId);
        }
        catch (SmtpException ex)
        {
            _logger.LogError(
                ex,
                "Failed to send invite email '{Subject}' for invite {InviteId}",
                subject,
                context.InviteId);
            throw;
        }
    }

    private SmtpClient CreateClient()
    {
        var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl
        };

        if (!string.IsNullOrEmpty(_options.Username) && !string.IsNullOrEmpty(_options.Password))
        {
            client.Credentials = new NetworkCredential(_options.Username, _options.Password);
        }

        return client;
    }

    private MailMessage CreateMessage(string recipient, string subject, string body)
    {
        var fromAddress = new MailAddress(_options.FromAddress, _options.FromDisplayName);
        var message = new MailMessage
        {
            Subject = subject,
            Body = body,
            BodyEncoding = Encoding.UTF8,
            From = fromAddress
        };

        message.To.Add(recipient);

        return message;
    }

    private static string BuildInviteBody(InviteEmailContext context)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Hi,");
        builder.AppendLine();
        builder.AppendLine("You've been invited to join a FotoTime group.");
        builder.AppendLine($"Invite token: {context.Token}");
        builder.AppendLine($"Expires at: {context.ExpiresAt:u}");
        builder.AppendLine();
        builder.AppendLine("Enter the token on the FotoTime site to accept your invite.");
        builder.AppendLine();
        builder.AppendLine("– The FotoTime Team");
        return builder.ToString();
    }

    private static string BuildReminderBody(InviteEmailContext context)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Hi,");
        builder.AppendLine();
        builder.AppendLine("Friendly reminder: your FotoTime invite is still waiting.");
        builder.AppendLine($"Invite token: {context.Token}");
        builder.AppendLine($"Expires at: {context.ExpiresAt:u}");
        builder.AppendLine();
        builder.AppendLine("Use the token on the FotoTime site to join before it expires.");
        builder.AppendLine();
        builder.AppendLine("– The FotoTime Team");
        return builder.ToString();
    }
}
