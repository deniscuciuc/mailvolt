using FluentAssertions;
using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using MailVolt.Transport.AzureEmail;
using MailVolt.Transport.AzureEmail.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace MailVolt.Transport.Tests;

public sealed class AzureEmailSenderTests
{
    [Fact]
    public void AzureEmailSender_implements_ISender()
    {
        typeof(AzureEmailSender).Should().Implement<ISender>();
    }

    [Fact]
    public void AddAzureEmailSender_with_delegate_registers_ISender()
    {
        var services = new ServiceCollection();
        var builder = new MailVoltBuilder(services);

        builder.AddAzureEmailSender(opts =>
        {
            opts.ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=testkey==";
        });

        var provider = services.BuildServiceProvider();
        var sender = provider.GetService<ISender>();
        sender.Should().NotBeNull();
        sender.Should().BeOfType<AzureEmailSender>();

        var resolvedOptions = provider.GetRequiredService<IOptions<AzureEmailSenderOptions>>().Value;
        resolvedOptions.ConnectionString.Should().Be("endpoint=https://test.communication.azure.com/;accesskey=testkey==");
    }

    [Fact]
    public void AddAzureEmailSender_with_configuration_registers_ISender()
    {
        var services = new ServiceCollection();
        var builder = new MailVoltBuilder(services);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MailVolt:Azure:ConnectionString"] = "endpoint=https://prod.communication.azure.com/;accesskey=prodkey=="
            })
            .Build();

        builder.AddAzureEmailSender(config);

        var provider = services.BuildServiceProvider();
        var sender = provider.GetService<ISender>();
        sender.Should().NotBeNull();
        sender.Should().BeOfType<AzureEmailSender>();

        var resolvedOptions = provider.GetRequiredService<IOptions<AzureEmailSenderOptions>>().Value;
        resolvedOptions.ConnectionString.Should().Be("endpoint=https://prod.communication.azure.com/;accesskey=prodkey==");
    }

    [Fact]
    public void AzureEmailSender_throws_on_null_options()
    {
        var act = () => new AzureEmailSender(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task SendAsync_returns_failure_when_From_is_missing()
    {
        // We need to create an AzureEmailSender with a valid connection string
        // to test the error path. Since EmailClient is sealed, we test the
        // validation path before the client is used.
        var options = Helpers.OptionsOf(new AzureEmailSenderOptions
        {
            ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=dGVzdA=="
        });

        var sender = new AzureEmailSender(options);
        var email = Helpers.CreateTestEmail() with { From = null };

        var result = await sender.SendAsync(email);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("From");
    }
}
