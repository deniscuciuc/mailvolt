using MailVolt.Core.Interfaces;
using MailVolt.Core.Transports;
using Microsoft.Extensions.DependencyInjection;

namespace MailVolt.Core.DependencyInjection;

/// <summary>
/// Extension methods for registering the in-memory transport.
/// </summary>
public static class InMemoryTransportExtensions
{
    /// <summary>
    /// Registers <see cref="InMemorySender"/> as the <see cref="ISender"/>, capturing
    /// messages instead of delivering them.
    /// </summary>
    /// <param name="builder">The MailVolt builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <remarks>
    /// Registered as a singleton so the same instance can be inspected after sending.
    /// </remarks>
    public static MailVoltBuilder UseInMemoryTransport(this MailVoltBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSingleton<InMemorySender>();
        builder.Services.AddSingleton<ISender>(sp => sp.GetRequiredService<InMemorySender>());
        return builder;
    }

    /// <summary>
    /// Registers <see cref="FailingSender"/> as the <see cref="ISender"/>, so every send
    /// fails. Useful for exercising a caller's error handling.
    /// </summary>
    /// <param name="builder">The MailVolt builder.</param>
    /// <param name="errorMessage">The error to report on each send.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static MailVoltBuilder UseFailingTransport(this MailVoltBuilder builder, string? errorMessage = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSingleton<ISender>(new FailingSender(errorMessage));
        return builder;
    }
}
