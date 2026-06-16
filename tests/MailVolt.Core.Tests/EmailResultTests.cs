using FluentAssertions;
using MailVolt.Core.Models;
using Xunit;

namespace MailVolt.Core.Tests;

public sealed class EmailResultTests
{
    [Fact]
    public void Success_creates_result_with_IsSuccess_true()
    {
        var result = EmailResult.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.MessageId.Should().BeNull();
        result.Error.Should().BeNull();
        result.Exception.Should().BeNull();
    }

    [Fact]
    public void Success_with_message_id_sets_message_id()
    {
        var result = EmailResult.Success("msg-123");

        result.IsSuccess.Should().BeTrue();
        result.MessageId.Should().Be("msg-123");
    }

    [Fact]
    public void Failure_creates_result_with_IsFailure_true()
    {
        var result = EmailResult.Failure("Something went wrong");

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Something went wrong");
        result.Exception.Should().BeNull();
        result.MessageId.Should().BeNull();
    }

    [Fact]
    public void Failure_with_exception_sets_exception()
    {
        var ex = new InvalidOperationException("test");
        var result = EmailResult.Failure("Error occurred", ex);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Error occurred");
        result.Exception.Should().BeSameAs(ex);
    }

    [Fact]
    public void Success_and_failure_are_not_equal()
    {
        var success = EmailResult.Success("id-1");
        var failure = EmailResult.Failure("error");

        success.Should().NotBe(failure);
    }

    [Fact]
    public void Two_success_results_with_same_message_id_are_equal()
    {
        var a = EmailResult.Success("msg-1");
        var b = EmailResult.Success("msg-1");

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Two_failure_results_with_same_error_are_equal()
    {
        var a = EmailResult.Failure("error");
        var b = EmailResult.Failure("error");

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void IsSuccess_and_IsFailure_are_mutually_exclusive()
    {
        var success = EmailResult.Success();
        var failure = EmailResult.Failure("err");

        success.IsSuccess.Should().BeTrue();
        success.IsFailure.Should().BeFalse();

        failure.IsSuccess.Should().BeFalse();
        failure.IsFailure.Should().BeTrue();
    }
}
