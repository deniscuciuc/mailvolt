using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace MailVolt.Testing.DependencyInjection;

/// <summary>
/// Extension methods for registering MailVolt testing utilities with DI.
/// </summary>
public static class TestingExtensions
{
    /// <summary>
    /// Replaces the real ISender with <see cref="InMemorySender"/> for testing.
    /// Registered as Singleton so the same instance can be inspected after sending.
    /// </summary>
    public static MailVoltBuilder UseInMemoryTransport(this MailVoltBuilder builder)
    {
        builder.Services.AddSingleton<InMemorySender>();
        builder.Services.AddSingleton<ISender>(sp => sp.GetRequiredService<InMemorySender>());
        return builder;
    }
}
