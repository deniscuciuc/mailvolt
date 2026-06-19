using System.Net.Http.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using MailKit.Security;
using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using MailVolt.Transport.Smtp.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MailVolt.Integration.Tests;

[Trait("Category", "Integration")]
public sealed class SmtpIntegrationTests : IAsyncLifetime
{
    private readonly IContainer _mailDev;

    public SmtpIntegrationTests()
    {
        _mailDev = new ContainerBuilder()
            .WithImage("maildev/maildev:latest")
            .WithPortBinding(1025, true)
            .WithPortBinding(1080, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1025))
            .Build();
    }

    public Task InitializeAsync() => _mailDev.StartAsync();

    public Task DisposeAsync() => _mailDev.DisposeAsync().AsTask();

    [Fact]
    public async Task SendAsync_ShouldDeliverEmail_WhenUsingRealSmtpContainer()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMailVolt()
            .UseSmtpTransport(options =>
            {
                options.Host = _mailDev.Hostname;
                options.Port = _mailDev.GetMappedPublicPort(1025);
                options.Security = SecureSocketOptions.None;
            });

        var provider = services.BuildServiceProvider();
        var builder = provider.GetRequiredService<IEmailBuilder>();

        // Act
        var result = await builder
            .From("sender@example.com")
            .To("recipient@example.com")
            .Subject("Integration test")
            .HtmlBody("<h1>Hello from MailVolt!</h1>")
            .TextBody("Hello from MailVolt!")
            .SendAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();

        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri($"http://{_mailDev.Hostname}:{_mailDev.GetMappedPublicPort(1080)}");
        var emails = await httpClient.GetFromJsonAsync<List<MaildevEmail>>("/email");
        emails.Should().ContainSingle();
        emails![0].To.Should().ContainSingle(x => x.Address == "recipient@example.com");
        emails[0].Subject.Should().Be("Integration test");
    }

    private sealed class MaildevEmail
    {
        public List<MaildevAddress> To { get; set; } = [];
        public string Subject { get; set; } = string.Empty;
    }

    private sealed class MaildevAddress
    {
        public string Address { get; set; } = string.Empty;
    }
}
