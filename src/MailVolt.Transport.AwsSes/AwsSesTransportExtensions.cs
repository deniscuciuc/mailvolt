namespace MailVolt.Transport.AwsSes.DependencyInjection;

using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering the AWS SES transport.
/// </summary>
public static class AwsSesTransportExtensions
{
    /// <summary>
    /// Registers the AWS SES email sender with the specified configuration delegate.
    /// </summary>
    /// <param name="builder">The <see cref="MailVoltBuilder"/> to add services to.</param>
    /// <param name="configure">A delegate to configure <see cref="AwsSesSenderOptions"/>.</param>
    /// <returns>The <see cref="MailVoltBuilder"/> for chaining.</returns>
    public static MailVoltBuilder UseAwsSesTransport(
        this MailVoltBuilder builder,
        Action<AwsSesSenderOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.Services.Configure(configure);
        builder.Services.AddTransient<ISender, AwsSesSender>();
        return builder;
    }

    /// <summary>
    /// Registers the AWS SES email sender, binding <see cref="AwsSesSenderOptions"/>
    /// from the <c>"MailVolt:AwsSes"</c> configuration section.
    /// </summary>
    /// <param name="builder">The <see cref="MailVoltBuilder"/> to add services to.</param>
    /// <param name="configuration">The configuration root.</param>
    /// <returns>The <see cref="MailVoltBuilder"/> for chaining.</returns>
    public static MailVoltBuilder UseAwsSesTransport(
        this MailVoltBuilder builder,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        builder.Services.Configure<AwsSesSenderOptions>(
            configuration.GetSection(AwsSesSenderOptions.SectionName));
        builder.Services.AddTransient<ISender, AwsSesSender>();
        return builder;
    }
}
