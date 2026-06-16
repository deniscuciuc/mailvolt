using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;

namespace MailVolt.Testing;

/// <summary>
/// ISender that always fails — for testing error handling scenarios.
/// </summary>
/// <param name="errorMessage">Optional custom error message. Defaults to "Simulated send failure".</param>
public sealed class FailingSender(string? errorMessage = null) : ISender
{
    /// <inheritdoc />
    public Task<EmailResult> SendAsync(EmailMessage email, CancellationToken cancellationToken = default) =>
        Task.FromResult(EmailResult.Failure(errorMessage ?? "Simulated send failure"));
}
