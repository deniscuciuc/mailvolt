using AwesomeAssertions;
using MailVolt.Core;
using MailVolt.Core.Interfaces;
using MailVolt.Core.Models;
using MailVolt.Core.Options;
using Microsoft.Extensions.Options;
using Xunit;

namespace MailVolt.Core.Tests;

/// <summary>
/// A builder resolved from DI is transient, but nothing stops a consumer from holding one
/// and building twice. These lock in that doing so cannot corrupt an earlier message.
/// </summary>
public sealed class EmailBuilderReuseTests
{
    private static EmailBuilder CreateBuilder() =>
        new(Microsoft.Extensions.Options.Options.Create(new MailVoltOptions()));

    [Fact]
    public async Task Building_twice_does_not_mutate_the_first_message()
    {
        var builder = CreateBuilder();
        builder.From("from@example.com").To("first@example.com").Subject("s").TextBody("b");

        var first = await builder.BuildAsync();
        builder.To("second@example.com");
        var second = await builder.BuildAsync();

        // AsReadOnly() would have returned a live view, so `first` would now list both.
        first.To.Should().ContainSingle(x => x.Address == "first@example.com");
        second.To.Should().HaveCount(2);
    }

    [Fact]
    public async Task Building_twice_does_not_mutate_the_first_message_tags_or_attachments()
    {
        var builder = CreateBuilder();
        builder.From("from@example.com")
            .To("to@example.com")
            .Subject("s")
            .TextBody("b")
            .Tag("first")
            .Attach(a => a.FromBytes("a.txt", "a"u8.ToArray()));

        var first = await builder.BuildAsync();
        builder.Tag("second").Attach(a => a.FromBytes("b.txt", "b"u8.ToArray()));
        var second = await builder.BuildAsync();

        first.Tags.Should().ContainSingle();
        first.Attachments.Should().ContainSingle();
        second.Tags.Should().HaveCount(2);
        second.Attachments.Should().HaveCount(2);
    }

    [Fact]
    public async Task Building_twice_yields_equivalent_messages_when_nothing_changed()
    {
        var builder = CreateBuilder();
        builder.From("from@example.com").To("to@example.com").Subject("s").HtmlBody("<p>b</p>");

        var first = await builder.BuildAsync();
        var second = await builder.BuildAsync();

        second.To.Should().BeEquivalentTo(first.To);
        second.HtmlBody.Should().Be(first.HtmlBody);
        second.From!.Address.Should().Be(first.From!.Address);
    }

    [Fact]
    public async Task An_attachment_can_be_read_more_than_once()
    {
        var builder = CreateBuilder();
        builder.From("from@example.com")
            .To("to@example.com")
            .Subject("s")
            .TextBody("b")
            .Attach(a => a.FromBytes("report.csv", "id,name"u8.ToArray()));

        var message = await builder.BuildAsync();
        var attachment = message.Attachments[0];

        // A Stream would be exhausted by the first read, silently sending an empty
        // attachment on a retry.
        attachment.Content.ToArray().Should().Equal("id,name"u8.ToArray());
        attachment.Content.ToArray().Should().Equal("id,name"u8.ToArray());

        using var firstStream = attachment.OpenReadStream();
        using var secondStream = attachment.OpenReadStream();
        new StreamReader(firstStream).ReadToEnd().Should().Be("id,name");
        new StreamReader(secondStream).ReadToEnd().Should().Be("id,name");
    }

    [Fact]
    public async Task FromFile_reads_the_file_and_leaves_no_handle_open()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mailvolt-{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(path, "file-content");

        try
        {
            var builder = CreateBuilder();
            builder.From("from@example.com")
                .To("to@example.com")
                .Subject("s")
                .TextBody("b")
                .Attach(a => a.FromFile(path));

            var message = await builder.BuildAsync();
            message.Attachments[0].Content.ToArray().Should().Equal("file-content"u8.ToArray());

            // FromFile used to hold an open FileStream, so this delete would fail on
            // Windows and leak a handle everywhere else.
            var act = () => File.Delete(path);
            act.Should().NotThrow();
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task AsInlineImage_keeps_a_detected_content_type()
    {
        var builder = CreateBuilder();
        builder.From("from@example.com")
            .To("to@example.com")
            .Subject("s")
            .HtmlBody("<p>x</p>")
            .Attach(a => a.FromBytes("logo.jpg", [1, 2, 3]).AsInlineImage("logo@mailvolt"));

        var message = await builder.BuildAsync();

        // Previously hardcoded to image/png, mislabelling every non-PNG inline image.
        message.Attachments[0].ContentType.Should().Be("image/jpeg");
        message.Attachments[0].IsInline.Should().BeTrue();
    }

    [Fact]
    public async Task AsInlineImage_still_defaults_to_png_for_an_unknown_extension()
    {
        var builder = CreateBuilder();
        builder.From("from@example.com")
            .To("to@example.com")
            .Subject("s")
            .HtmlBody("<p>x</p>")
            .Attach(a => a.FromBytes("logo", [1, 2, 3]).AsInlineImage("logo@mailvolt"));

        var message = await builder.BuildAsync();

        message.Attachments[0].ContentType.Should().Be("image/png");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-at-sign")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user@nodot")]
    [InlineData("user name@example.com")]
    [InlineData("a@b.com,c@d.com")]
    public void An_invalid_address_is_rejected(string? address)
    {
        EmailAddress.IsValid(address).Should().BeFalse();
        EmailAddress.TryParse(address, out var parsed).Should().BeFalse();
        parsed.Should().BeNull();
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("first.last+tag@sub.example.co.uk")]
    [InlineData("  padded@example.com  ")]
    public void A_valid_address_is_accepted(string address)
    {
        EmailAddress.IsValid(address).Should().BeTrue();
        EmailAddress.TryParse(address, out var parsed).Should().BeTrue();
        parsed!.Address.Should().Be(address.Trim());
    }

    [Fact]
    public void Constructing_an_invalid_address_throws()
    {
        var act = () => new EmailAddress("garbage");

        act.Should().Throw<ArgumentException>().WithMessage("*not a valid email address*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task A_non_positive_MaxConcurrency_is_rejected(int maxConcurrency)
    {
        var sender = new BatchEmailSender(new NoopSender());

        var act = () => sender.SendBatchAsync(
            [Message()],
            new BatchSendOptions(MaxConcurrency: maxConcurrency));

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task A_negative_delay_is_rejected()
    {
        var sender = new BatchEmailSender(new NoopSender());

        var act = () => sender.SendBatchAsync([Message()], new BatchSendOptions(DelayMs: -1));

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task Counts_always_sum_to_the_total_when_the_batch_stops_early()
    {
        var sender = new BatchEmailSender(new FailOnSecondSender());

        var result = await sender.SendBatchAsync(
            [Message(), Message(), Message(), Message(), Message()],
            new BatchSendOptions(
                MaxConcurrency: 1,
                FailureStrategy: FailureStrategy.StopOnFirstFailure));

        result.TotalCount.Should().Be(5);
        result.SkippedCount.Should().BeGreaterThan(0);
        (result.SentCount + result.FailedCount + result.SkippedCount)
            .Should().Be(result.TotalCount);
    }

    [Fact]
    public async Task Nothing_is_skipped_when_every_send_succeeds()
    {
        var sender = new BatchEmailSender(new NoopSender());

        var result = await sender.SendBatchAsync(
            [Message(), Message(), Message()],
            new BatchSendOptions(FailureStrategy: FailureStrategy.Continue));

        result.SentCount.Should().Be(3);
        result.SkippedCount.Should().Be(0);
        result.HasFailures.Should().BeFalse();
    }

    private static EmailMessage Message() => new()
    {
        From = "from@example.com",
        To = [new EmailAddress("to@example.com")],
        Subject = "s",
        TextBody = "b",
    };

    private sealed class NoopSender : ISender
    {
        public Task<EmailResult> SendAsync(EmailMessage email, CancellationToken cancellationToken = default) =>
            Task.FromResult(EmailResult.Success("id"));
    }

    private sealed class FailOnSecondSender : ISender
    {
        private int _count;

        public Task<EmailResult> SendAsync(EmailMessage email, CancellationToken cancellationToken = default) =>
            Task.FromResult(Interlocked.Increment(ref _count) == 2
                ? EmailResult.Failure("boom")
                : EmailResult.Success("id"));
    }
}
