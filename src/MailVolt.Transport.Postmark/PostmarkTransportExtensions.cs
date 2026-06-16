using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using MailVolt.Transport.Postmark;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering the Postmark transport with the MailVolt pipeline.
/// </summary>
public static class PostmarkTransportExtensions
{
    /// <summary>
    /// Registers the Postmark email transport as the <see cref="ISender"/> implementation.
    /// </summary>
    /// <param name="builder">The <see cref="MailVoltBuilder"/> returned by <c>AddMailVolt</c>.</param>
    /// <param name="configureOptions">An optional delegate to configure <see cref="PostmarkSenderOptions"/>.</param>
    /// <returns>The builder instance for chaining.</returns>
    public static MailVoltBuilder AddPostmarkSender(
        this MailVoltBuilder builder,
        Action<PostmarkSenderOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (configureOptions is not null)
        {
            builder.Services.Configure(configureOptions);
        }
        else
        {
            builder.Services.Configure<PostmarkSenderOptions>(_ => { });
        }

        RegisterPostmarkServices(builder.Services);

        return builder;
    }

    /// <summary>
    /// Registers the Postmark email transport as the <see cref="ISender"/> implementation,
    /// binding <see cref="PostmarkSenderOptions"/> from the <c>"MailVolt:Postmark"</c> configuration section.
    /// </summary>
    /// <param name="builder">The <see cref="MailVoltBuilder"/> returned by <c>AddMailVolt</c>.</param>
    /// <param name="configuration">The configuration root. The <c>"MailVolt:Postmark"</c> section will be bound to <see cref="PostmarkSenderOptions"/>.</param>
    /// <returns>The builder instance for chaining.</returns>
    public static MailVoltBuilder AddPostmarkSender(
        this MailVoltBuilder builder,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        builder.Services.Configure<PostmarkSenderOptions>(
            configuration.GetSection(PostmarkSenderOptions.SectionName));

        RegisterPostmarkServices(builder.Services);

        return builder;
    }

    /// <summary>
    /// Registers Postmark services without configuration binding (for when options are already registered via other means).
    /// </summary>
    /// <param name="builder">The <see cref="MailVoltBuilder"/> returned by <c>AddMailVolt</c>.</param>
    /// <returns>The builder instance for chaining.</returns>
    public static MailVoltBuilder AddPostmarkSender(this MailVoltBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        RegisterPostmarkServices(builder.Services);

        return builder;
    }

    /// <summary>
    /// Common registration logic shared by all overloads.
    /// </summary>
    private static void RegisterPostmarkServices(IServiceCollection services)
    {
        // Named HttpClient for Postmark
        services.AddHttpClient(PostmarkHttpClient.Name, (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<PostmarkSenderOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/'));
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "X-Postmark-Server-Token", options.ApiKey);
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        });

        // Register the sender as both IPostmarkSender and ISender
        services.AddTransient<IPostmarkSender, PostmarkSender>();
        services.AddTransient<ISender>(sp => sp.GetRequiredService<IPostmarkSender>());
    }
}
