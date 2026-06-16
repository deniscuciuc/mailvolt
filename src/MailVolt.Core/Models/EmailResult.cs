namespace MailVolt.Core.Models;

/// <summary>
/// Represents the result of sending a single email.
/// </summary>
public sealed record EmailResult
{
    private EmailResult()
    {
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="messageId">Optional provider-specific message identifier.</param>
    /// <returns>A new <see cref="EmailResult"/> indicating success.</returns>
    public static EmailResult Success(string? messageId = null) =>
        new()
        {
            IsSuccess = true,
            MessageId = messageId,
        };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="error">A description of the error.</param>
    /// <param name="exception">Optional exception that caused the failure.</param>
    /// <returns>A new <see cref="EmailResult"/> indicating failure.</returns>
    public static EmailResult Failure(string error, Exception? exception = null) =>
        new()
        {
            IsFailure = true,
            Error = error,
            Exception = exception,
        };

    /// <summary>
    /// Whether the email was sent successfully.
    /// </summary>
    public bool IsSuccess { get; private init; }

    /// <summary>
    /// Whether sending the email failed.
    /// </summary>
    public bool IsFailure { get; private init; }

    /// <summary>
    /// Provider-specific message identifier, set when <see cref="IsSuccess"/> is true.
    /// </summary>
    public string? MessageId { get; private init; }

    /// <summary>
    /// Error description, set when <see cref="IsFailure"/> is true.
    /// </summary>
    public string? Error { get; private init; }

    /// <summary>
    /// Exception that caused the failure, if any.
    /// </summary>
    public Exception? Exception { get; private init; }
}
