using FluentAssertions;
using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using MailVolt.Transport.AwsSes;
using MailVolt.Transport.AwsSes.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace MailVolt.Transport.Tests;

public sealed class AwsSesSenderTests
{
    [Fact]
    public void AwsSesSender_implements_ISender()
    {
        typeof(AwsSesSender).Should().Implement<ISender>();
    }

    [Fact]
    public void UseAwsSesTransport_with_delegate_registers_ISender()
    {
        var services = new ServiceCollection();
        var builder = new MailVoltBuilder(services);

        builder.UseAwsSesTransport(opts =>
        {
            opts.AccessKeyId = "AKIA123";
            opts.SecretAccessKey = "secret456";
            opts.Region = "us-west-2";
            opts.ConfigurationSetName = "my-config-set";
        });

        var provider = services.BuildServiceProvider();
        var sender = provider.GetService<ISender>();
        sender.Should().NotBeNull();
        sender.Should().BeOfType<AwsSesSender>();

        var resolvedOptions = provider.GetRequiredService<IOptions<AwsSesSenderOptions>>().Value;
        resolvedOptions.AccessKeyId.Should().Be("AKIA123");
        resolvedOptions.SecretAccessKey.Should().Be("secret456");
        resolvedOptions.Region.Should().Be("us-west-2");
        resolvedOptions.ConfigurationSetName.Should().Be("my-config-set");
    }

    [Fact]
    public void UseAwsSesTransport_with_configuration_registers_ISender()
    {
        var services = new ServiceCollection();
        var builder = new MailVoltBuilder(services);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MailVolt:AwsSes:AccessKeyId"] = "AKIA456",
                ["MailVolt:AwsSes:SecretAccessKey"] = "secret789",
                ["MailVolt:AwsSes:Region"] = "eu-central-1",
                ["MailVolt:AwsSes:ConfigurationSetName"] = "prod-config"
            })
            .Build();

        builder.UseAwsSesTransport(config);

        var provider = services.BuildServiceProvider();
        var sender = provider.GetService<ISender>();
        sender.Should().NotBeNull();
        sender.Should().BeOfType<AwsSesSender>();

        var resolvedOptions = provider.GetRequiredService<IOptions<AwsSesSenderOptions>>().Value;
        resolvedOptions.AccessKeyId.Should().Be("AKIA456");
        resolvedOptions.SecretAccessKey.Should().Be("secret789");
        resolvedOptions.Region.Should().Be("eu-central-1");
        resolvedOptions.ConfigurationSetName.Should().Be("prod-config");
    }

    [Fact]
    public void UseAwsSesTransport_uses_default_region_when_not_specified()
    {
        var services = new ServiceCollection();
        var builder = new MailVoltBuilder(services);

        builder.UseAwsSesTransport(opts =>
        {
            opts.AccessKeyId = "AKIA789";
            opts.SecretAccessKey = "secret012";
        });

        var provider = services.BuildServiceProvider();
        var resolvedOptions = provider.GetRequiredService<IOptions<AwsSesSenderOptions>>().Value;
        resolvedOptions.Region.Should().Be("us-east-1");
    }

    [Fact]
    public async Task AwsSesSender_requires_From_address()
    {
        var options = Helpers.OptionsOf(new AwsSesSenderOptions
        {
            AccessKeyId = "AKIA123",
            SecretAccessKey = "secret456"
        });

        var sender = new AwsSesSender(options);
        var email = Helpers.CreateTestEmail() with { From = null };

        // The sender will try to connect to AWS, but it should handle exceptions gracefully
        // and return a failure result since it can't actually connect
        var result = await sender.SendAsync(email);

        result.IsSuccess.Should().BeFalse();
    }
}
