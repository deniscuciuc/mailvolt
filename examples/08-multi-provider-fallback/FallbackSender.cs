using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Tries primarySender first. If it fails, logs a warning and tries fallbackSender.
/// </summary>
public sealed class FallbackSender(
    ISender primarySender,
    ISender fallbackSender,
    ILogger<FallbackSender> logger) : ISender
{
    public async Task<EmailResult> SendAsync(EmailMessage email, CancellationToken ct = default)
    {
        var primary = await primarySender.SendAsync(email, ct);

        if (primary.IsSuccess)
            return primary;

        logger.LogWarning(
            "Primary sender failed ({Error}). Trying fallback...", primary.Error);

        var fallback = await fallbackSender.SendAsync(email, ct);

        if (fallback.IsSuccess)
            logger.LogInformation("Fallback sender succeeded. MessageId: {Id}", fallback.MessageId);
        else
            logger.LogError("Both primary and fallback failed. Last error: {Error}", fallback.Error);

        return fallback;
    }
}
