using AwesomeAssertions;
using MailVolt.Core.Models;
using Xunit;

namespace MailVolt.Core.Tests;

public sealed class EmailAddressTests
{
    [Fact]
    public void Constructor_sets_address_and_display_name()
    {
        var address = new EmailAddress("user@example.com", "John Doe");

        address.Address.Should().Be("user@example.com");
        address.DisplayName.Should().Be("John Doe");
    }

    [Fact]
    public void Constructor_defaults_display_name_to_null()
    {
        var address = new EmailAddress("user@example.com");

        address.Address.Should().Be("user@example.com");
        address.DisplayName.Should().BeNull();
    }

    [Fact]
    public void ToString_returns_plain_address_when_no_display_name()
    {
        var address = new EmailAddress("user@example.com");

        address.ToString().Should().Be("user@example.com");
    }

    [Fact]
    public void ToString_returns_formatted_with_display_name()
    {
        var address = new EmailAddress("user@example.com", "John Doe");

        address.ToString().Should().Be("John Doe <user@example.com>");
    }

    [Fact]
    public void ToString_handles_empty_display_name()
    {
        var address = new EmailAddress("user@example.com", string.Empty);

        address.ToString().Should().Be("user@example.com");
    }

    [Fact]
    public void Implicit_conversion_from_string_creates_email_address()
    {
        EmailAddress address = "user@example.com";

        address.Address.Should().Be("user@example.com");
        address.DisplayName.Should().BeNull();
    }

    [Fact]
    public void Equality_same_address_are_equal()
    {
        var a = new EmailAddress("user@example.com", "John");
        var b = new EmailAddress("user@example.com", "John");

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equality_different_address_are_not_equal()
    {
        var a = new EmailAddress("user1@example.com");
        var b = new EmailAddress("user2@example.com");

        a.Should().NotBe(b);
    }

    [Fact]
    public void Equality_different_display_name_are_not_equal()
    {
        var a = new EmailAddress("user@example.com", "John");
        var b = new EmailAddress("user@example.com", "Jane");

        a.Should().NotBe(b);
    }

    [Fact]
    public void Equality_compares_using_record_semantics()
    {
        var addresses = new[]
        {
            new EmailAddress("a@b.com", "A"),
            new EmailAddress("a@b.com", "A"),
            new EmailAddress("b@c.com", "B"),
        };

        addresses[0].Should().Be(addresses[1]);
        addresses[0].Should().NotBe(addresses[2]);
        addresses[1].Should().NotBe(addresses[2]);
    }

    [Fact]
    public void Can_use_in_a_list_as_expected()
    {
        var list = new List<EmailAddress>
        {
            new("a@b.com"),
            new("b@c.com"),
            new("a@b.com"),
        };

        list.Should().HaveCount(3);
        list.Distinct().Should().HaveCount(2);
    }
}
