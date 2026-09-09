using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using MailVolt.Core.Transports;

namespace MailVolt.Core.Transports;

/// <summary>
/// An <see cref="ISender"/> that always fails, for exercising a caller's error handling.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="FailingSender"/> class.
/// </remarks>
/// <param name="errorMessage">The error to report. Defaults to a generic message.</param>
public sealed class FailingSender(string? errorMessage = null) : ISender
{
    private readonly string _errorMessage = errorMessage ?? "Simulated send failure.";

    /// <inheritdoc />
    public Task<EmailResult> SendAsync(EmailMessage email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(EmailResult.Failure(_errorMessage));
    }
}
