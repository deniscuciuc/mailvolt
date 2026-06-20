using AwesomeAssertions;
using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using MailVolt.Testing;
using NSubstitute;
using Xunit;

namespace MailVolt.Core.Tests;

public sealed class BatchEmailSenderTests
{
    private static EmailMessage CreateEmail(string to, string subject = "Test") => new()
    {
        From = new EmailAddress("from@example.com"),
        To = new[] { new EmailAddress(to) }.ToList().AsReadOnly(),
        Subject = subject,
    };

    private static BatchSendOptions DefaultOptions() => new(
        MaxConcurrency: 2,
        DelayMs: null,
        FailureStrategy: FailureStrategy.Continue);

    // ─── Sends all emails ──────────────────────────────────────────────

    [Fact]
    public async Task SendBatchAsync_sends_all_emails()
    {
        var sender = new InMemorySender();
        var batchSender = new BatchEmailSender(sender);

        var emails = new List<EmailMessage>
        {
            CreateEmail("a@test.com"),
            CreateEmail("b@test.com"),
            CreateEmail("c@test.com"),
        };

        var result = await batchSender.SendBatchAsync(emails, DefaultOptions());

        result.TotalCount.Should().Be(3);
        result.SentCount.Should().Be(3);
        result.FailedCount.Should().Be(0);
        result.HasFailures.Should().BeFalse();
        sender.SentCount.Should().Be(3);
    }

    [Fact]
    public async Task SendBatchAsync_returns_empty_result_for_empty_list()
    {
        var sender = new InMemorySender();
        var batchSender = new BatchEmailSender(sender);

        var result = await batchSender.SendBatchAsync([], DefaultOptions());

        result.TotalCount.Should().Be(0);
        result.SentCount.Should().Be(0);
        result.FailedCount.Should().Be(0);
        result.HasFailures.Should().BeFalse();
        result.Results.Should().BeEmpty();
        sender.SentCount.Should().Be(0);
    }

    // ─── Concurrency ───────────────────────────────────────────────────

    [Fact]
    public async Task SendBatchAsync_respects_concurrency_limit()
    {
        var sender = Substitute.For<ISender>();
        var concurrencyCounter = 0;
        var maxObservedConcurrency = 0;

        sender.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                var current = Interlocked.Increment(ref concurrencyCounter);
                InterlockedExchangeMax(ref maxObservedConcurrency, current);
                await Task.Delay(100);
                Interlocked.Decrement(ref concurrencyCounter);
                return EmailResult.Success();
            });

        var batchSender = new BatchEmailSender(sender);
        var emails = Enumerable.Range(0, 10)
            .Select(i => CreateEmail($"user{i}@test.com"))
            .ToList();

        var options = new BatchSendOptions(MaxConcurrency: 3);

        var result = await batchSender.SendBatchAsync(emails, options);

        result.TotalCount.Should().Be(10);
        result.SentCount.Should().Be(10);
        maxObservedConcurrency.Should().BeLessThanOrEqualTo(3);
    }

    // ─── ContinueOnFailure ─────────────────────────────────────────────

    [Fact]
    public async Task SendBatchAsync_ContinueOnFailure_sends_all_emails_even_with_failures()
    {
        var sender = Substitute.For<ISender>();
        var callCount = 0;

        sender.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                var count = Interlocked.Increment(ref callCount);
                return count % 2 == 0
                    ? EmailResult.Failure("Simulated failure")
                    : EmailResult.Success();
            });

        var batchSender = new BatchEmailSender(sender);
        var emails = Enumerable.Range(0, 6)
            .Select(i => CreateEmail($"user{i}@test.com"))
            .ToList();

        var options = new BatchSendOptions(
            MaxConcurrency: 2,
            FailureStrategy: FailureStrategy.Continue);

        var result = await batchSender.SendBatchAsync(emails, options);

        result.TotalCount.Should().Be(6);
        result.SentCount.Should().Be(3);
        result.FailedCount.Should().Be(3);
        result.HasFailures.Should().BeTrue();
        result.Results.Should().HaveCount(6);
    }

    // ─── StopOnFirstFailure ────────────────────────────────────────────

    [Fact]
    public async Task SendBatchAsync_StopOnFirstFailure_stops_after_first_failure()
    {
        var sender = new FailingSender("First failure");
        var batchSender = new BatchEmailSender(sender);

        var emails = Enumerable.Range(0, 10)
            .Select(i => CreateEmail($"user{i}@test.com"))
            .ToList();

        var options = new BatchSendOptions(
            MaxConcurrency: 2,
            FailureStrategy: FailureStrategy.StopOnFirstFailure);

        var result = await batchSender.SendBatchAsync(emails, options);

        // With StopOnFirstFailure and all failing, only the first batch (up to MaxConcurrency)
        // should have been attempted before cancellation propagates
        result.HasFailures.Should().BeTrue();
        result.FailedCount.Should().BeGreaterThan(0);
        result.SentCount.Should().Be(0);
        // The total count should still represent the original list
        result.TotalCount.Should().Be(10);
    }

    [Fact]
    public async Task SendBatchAsync_StopOnFirstFailure_with_mixed_results()
    {
        var sender = Substitute.For<ISender>();
        var callCount = 0;

        sender.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                var count = Interlocked.Increment(ref callCount);
                // First 3 succeed, 4th fails
                return count <= 3
                    ? EmailResult.Success($"msg-{count}")
                    : EmailResult.Failure($"Failure #{count}");
            });

        var batchSender = new BatchEmailSender(sender);
        var emails = Enumerable.Range(0, 6)
            .Select(i => CreateEmail($"user{i}@test.com"))
            .ToList();

        var options = new BatchSendOptions(
            MaxConcurrency: 1,
            FailureStrategy: FailureStrategy.StopOnFirstFailure);

        var result = await batchSender.SendBatchAsync(emails, options);

        // First 3 succeed, 4th fails -> stops
        result.SentCount.Should().Be(3);
        result.FailedCount.Should().Be(1);
        result.TotalCount.Should().Be(6);
        result.HasFailures.Should().BeTrue();
        result.Results.Should().HaveCount(4);
    }

    // ─── DelayMs ───────────────────────────────────────────────────────

    [Fact]
    public async Task SendBatchAsync_respects_delay_between_sends()
    {
        var sender = new InMemorySender();
        var batchSender = new BatchEmailSender(sender);

        var emails = Enumerable.Range(0, 3)
            .Select(i => CreateEmail($"user{i}@test.com"))
            .ToList();

        var options = new BatchSendOptions(
            MaxConcurrency: 1,
            DelayMs: 50,
            FailureStrategy: FailureStrategy.Continue);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await batchSender.SendBatchAsync(emails, options);
        sw.Stop();

        result.TotalCount.Should().Be(3);
        result.SentCount.Should().Be(3);
        // With delay of 50ms between 3 sends (2 delays) with concurrency 1,
        // it should take at least 100ms
        sw.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(90);
    }

    // ─── Null arguments ────────────────────────────────────────────────

    [Fact]
    public async Task SendBatchAsync_throws_on_null_emails()
    {
        var sender = new InMemorySender();
        var batchSender = new BatchEmailSender(sender);

        var act = () => batchSender.SendBatchAsync(null!, DefaultOptions());

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SendBatchAsync_throws_on_null_options()
    {
        var sender = new InMemorySender();
        var batchSender = new BatchEmailSender(sender);
        var emails = new List<EmailMessage> { CreateEmail("a@test.com") };

        var act = () => batchSender.SendBatchAsync(emails, null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ─── Results contain original messages ─────────────────────────────

    [Fact]
    public async Task SendBatchAsync_returns_results_with_original_messages()
    {
        var sender = new InMemorySender();
        var batchSender = new BatchEmailSender(sender);

        var email1 = CreateEmail("first@test.com", "First");
        var email2 = CreateEmail("second@test.com", "Second");
        var emails = new List<EmailMessage> { email1, email2 };

        var result = await batchSender.SendBatchAsync(emails, DefaultOptions());

        result.Results.Should().HaveCount(2);
        result.Results[0].Message.Subject.Should().Be("First");
        result.Results[1].Message.Subject.Should().Be("Second");
        result.Results[0].Result.IsSuccess.Should().BeTrue();
        result.Results[1].Result.IsSuccess.Should().BeTrue();
    }

    // ─── Cancellation ──────────────────────────────────────────────────

    [Fact]
    public async Task SendBatchAsync_respects_cancellation_token()
    {
        var sender = Substitute.For<ISender>();
        sender.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await Task.Delay(500);
                return EmailResult.Success();
            });

        var batchSender = new BatchEmailSender(sender);
        var emails = Enumerable.Range(0, 5)
            .Select(i => CreateEmail($"user{i}@test.com"))
            .ToList();

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        var act = () => batchSender.SendBatchAsync(emails, DefaultOptions(), cts.Token);

        (await act.Should().ThrowAsync<OperationCanceledException>())
            .And.CancellationToken.Should().Be(cts.Token);
    }

    private static void InterlockedExchangeMax(ref int target, int value)
    {
        int initial;
        do
        {
            initial = target;
            if (value <= initial) break;
        }
        while (Interlocked.CompareExchange(ref target, value, initial) != initial);
    }
}
