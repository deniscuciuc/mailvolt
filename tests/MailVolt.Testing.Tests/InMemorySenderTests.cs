using AwesomeAssertions;
using MailVolt.Core.DependencyInjection;
using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using MailVolt.Core.Transports;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MailVolt.Testing.Tests;

public sealed class InMemorySenderTests
{
    [Fact]
    public async Task A_sent_email_is_captured_with_a_timestamp()
    {
        var sender = new InMemorySender();
        var before = DateTimeOffset.UtcNow;

        var result = await sender.SendAsync(Email());

        result.IsSuccess.Should().BeTrue();
        result.MessageId.Should().NotBeNullOrWhiteSpace();
        sender.SentCount.Should().Be(1);
        sender.SentEmails[0].SentAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public async Task Emails_are_captured_in_order()
    {
        var sender = new InMemorySender();

        await sender.SendAsync(Email() with { Subject = "first" });
        await sender.SendAsync(Email() with { Subject = "second" });

        sender.SentEmails.Select(s => s.Email.Subject).Should().Equal("first", "second");
    }

    [Fact]
    public async Task Clear_removes_every_captured_email()
    {
        var sender = new InMemorySender();
        await sender.SendAsync(Email());

        sender.Clear();

        sender.SentCount.Should().Be(0);
        sender.SentEmails.Should().BeEmpty();
    }

    [Fact]
    public async Task Concurrent_sends_are_all_captured()
    {
        var sender = new InMemorySender();

        await Task.WhenAll(Enumerable.Range(0, 200).Select(i =>
            sender.SendAsync(Email() with { Subject = $"s{i}" })));

        sender.SentCount.Should().Be(200);
        sender.SentEmails.Select(s => s.Email.Subject).Distinct().Should().HaveCount(200);
    }

    [Fact]
    public async Task A_cancelled_send_throws_and_captures_nothing()
    {
        var sender = new InMemorySender();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => sender.SendAsync(Email(), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        sender.SentCount.Should().Be(0);
    }

    [Fact]
    public async Task FailingSender_always_reports_a_failure()
    {
        var sender = new FailingSender();

        var result = await sender.SendAsync(Email());

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task FailingSender_reports_the_configured_message()
    {
        var sender = new FailingSender("provider rejected the request");

        var result = await sender.SendAsync(Email());

        result.Error.Should().Be("provider rejected the request");
    }

    [Fact]
    public void UseInMemoryTransport_registers_one_shared_instance()
    {
        var services = new ServiceCollection();
        services.AddMailVolt().UseInMemoryTransport();

        using var provider = services.BuildServiceProvider();

        // The same instance must back both, or a test could not inspect what the code
        // under test sent through ISender.
        provider.GetRequiredService<ISender>()
            .Should().BeSameAs(provider.GetRequiredService<InMemorySender>());
    }

    [Fact]
    public async Task UseFailingTransport_registers_a_sender_that_fails()
    {
        var services = new ServiceCollection();
        services.AddMailVolt().UseFailingTransport("nope");

        using var provider = services.BuildServiceProvider();
        var result = await provider.GetRequiredService<ISender>().SendAsync(Email());

        result.Error.Should().Be("nope");
    }

    private static EmailMessage Email() => new()
    {
        From = new EmailAddress("from@example.com"),
        To = [new EmailAddress("to@example.com")],
        Subject = "Subject",
        TextBody = "body",
    };
}
