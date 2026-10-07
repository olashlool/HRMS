using HRMS.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace HRMS.Infrastructure.Authentication;

public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(
        string toEmail,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "DEVELOPMENT EMAIL (not delivered) to {ToEmail} | {Subject} | {Body}",
            toEmail,
            subject,
            body);

        return Task.CompletedTask;
    }
}
