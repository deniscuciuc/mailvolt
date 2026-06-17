using FluentAssertions;
using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using MailVolt.Core.Options;
using MailVolt.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace MailVolt.Core.Tests;

public sealed class MailVoltServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMailVolt_registers_IEmailBuilder()
    {
        var services = new ServiceCollection();

        services.AddMailVolt();

        var descriptor = services.SingleOrDefault(s => s.ServiceType == typeof(IEmailBuilder));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Transient);
        descriptor.ImplementationType.Should().Be(typeof(EmailBuilder));
    }

    [Fact]
    public void AddMailVolt_registers_IBatchEmailSender()
    {
        var services = new ServiceCollection();

        services.AddMailVolt();

        var descriptor = services.SingleOrDefault(s => s.ServiceType == typeof(IBatchEmailSender));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Transient);
        descriptor.ImplementationType.Should().Be(typeof(BatchEmailSender));
    }

    [Fact]
    public void AddMailVolt_configures_MailVoltOptions()
    {
        var services = new ServiceCollection();

        services.AddMailVolt();

        var descriptor = services.SingleOrDefault(s => s.ServiceType == typeof(IConfigureOptions<MailVoltOptions>));
        descriptor.Should().NotBeNull();
    }

    [Fact]
    public void AddMailVolt_with_configure_options_applies_options()
    {
        var services = new ServiceCollection();

        services.AddMailVolt(options =>
        {
            options.DefaultFromAddress = "noreply@example.com";
            options.DefaultFromDisplayName = "No Reply";
        });

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<MailVoltOptions>>().Value;

        options.DefaultFromAddress.Should().Be("noreply@example.com");
        options.DefaultFromDisplayName.Should().Be("No Reply");
    }

    [Fact]
    public void AddMailVolt_returns_MailVoltBuilder()
    {
        var services = new ServiceCollection();

        var builder = services.AddMailVolt();

        builder.Should().NotBeNull();
        builder.Should().BeOfType<MailVoltBuilder>();
        builder.Services.Should().BeSameAs(services);
    }

    [Fact]
    public void MailVoltBuilder_chain_allows_further_registration()
    {
        var services = new ServiceCollection();

        var builder = services.AddMailVolt();

        // Simulate chaining — builder.Services gives access to the original collection
        var servicesAfter = builder.Services;
        servicesAfter.Should().BeSameAs(services);
    }

    [Fact]
    public void AddMailVolt_configures_empty_options_when_no_delegate()
    {
        var services = new ServiceCollection();

        services.AddMailVolt();

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<MailVoltOptions>>().Value;

        options.DefaultFromAddress.Should().BeNull();
        options.DefaultFromDisplayName.Should().BeNull();
    }

    [Fact]
    public void AddMailVolt_with_configuration_binds_options()
    {
        var services = new ServiceCollection();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MailVolt:DefaultFromAddress"] = "config@example.com",
                ["MailVolt:DefaultFromDisplayName"] = "Config Sender",
            })
            .Build();

        services.AddMailVolt(config);

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<MailVoltOptions>>().Value;

        options.DefaultFromAddress.Should().Be("config@example.com");
        options.DefaultFromDisplayName.Should().Be("Config Sender");
    }

    [Fact]
    public void AddMailVolt_with_configuration_registers_services()
    {
        var services = new ServiceCollection();

        var config = new ConfigurationBuilder().Build();
        services.AddMailVolt(config);
        services.AddSingleton<ISender>(new InMemorySender());

        var serviceProvider = services.BuildServiceProvider();

        var emailBuilder = serviceProvider.GetService<IEmailBuilder>();
        emailBuilder.Should().NotBeNull();

        var batchSender = serviceProvider.GetService<IBatchEmailSender>();
        batchSender.Should().NotBeNull();
    }

    [Fact]
    public void AddMailVolt_throws_on_null_services()
    {
        var act = () => MailVoltServiceCollectionExtensions.AddMailVolt(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddMailVolt_with_configuration_throws_on_null_services()
    {
        var config = new ConfigurationBuilder().Build();
        var act = () => MailVoltServiceCollectionExtensions.AddMailVolt(null!, config);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddMailVolt_with_configuration_throws_on_null_configuration()
    {
        var services = new ServiceCollection();
        var act = () => services.AddMailVolt((IConfiguration)null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
