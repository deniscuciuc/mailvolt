using AwesomeAssertions;
using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Models;
using MailVolt.Testing;
using MailVolt.Testing.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class UserServiceTests : IDisposable
{
    private readonly ServiceProvider _services;
    private readonly InMemorySender _sender;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        var sc = new ServiceCollection();
        sc.AddLogging();

        sc.AddMailVolt(opts => opts.DefaultFromAddress = "noreply@test.com")
          .UseInMemoryTransport();

        // UserService depends on IEmailBuilder, which is Transient — resolved normally
        sc.AddTransient<UserService>();

        _services = sc.BuildServiceProvider();
        _sender = _services.GetRequiredService<InMemorySender>();
        _sut = _services.GetRequiredService<UserService>();
    }

    [Fact]
    public async Task Register_SendsWelcomeEmail_ToCorrectAddress()
    {
        // Act
        var success = await _sut.RegisterAsync("denis@example.com", "Denis");

        // Assert
        success.Should().BeTrue();
        _sender.Should().HaveCount(1);
        _sender.Should().ContainEmailTo("denis@example.com");
        _sender.Should().ContainSubject("Welcome");
    }

    [Fact]
    public async Task Register_SendsExactlyOneEmail()
    {
        await _sut.RegisterAsync("alice@example.com", "Alice");
        await _sut.RegisterAsync("bob@example.com", "Bob");

        _sender.SentEmails.Should().HaveCount(2);
    }

    [Fact]
    public async Task SendPasswordReset_UsesHighPriority()
    {
        await _sut.SendPasswordResetAsync("user@example.com", "https://app/reset?token=abc");

        var sent = _sender.SentEmails.Single();
        sent.Email.Priority.Should().Be(EmailPriority.High);
        sent.Email.HtmlBody.Should().Contain("abc");
    }

    [Fact]
    public async Task NoSideEffects_BetweenTests()
    {
        // Each test creates fresh services — InMemorySender starts empty
        _sender.Should().HaveNoEmailsSent();
    }

    public void Dispose() => _services.Dispose();
}
