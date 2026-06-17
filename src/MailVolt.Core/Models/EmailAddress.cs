namespace MailVolt.Core.Models;

/// <summary>
/// Represents an email address with an optional display name.
/// </summary>
/// <param name="Address">The email address (e.g. user@example.com).</param>
/// <param name="DisplayName">Optional display name shown alongside the address.</param>
public sealed record EmailAddress(string Address, string? DisplayName = null)
{
    /// <summary>
    /// Formats the address as a string. If a <see cref="DisplayName"/> is present,
    /// returns <c>"DisplayName &lt;Address&gt;"</c>; otherwise returns the plain address.
    /// </summary>
    public override string ToString() =>
        DisplayName is { Length: > 0 } ? $"{DisplayName} <{Address}>" : Address;

    /// <summary>
    /// Implicitly converts a string to an <see cref="EmailAddress"/> with no display name.
    /// </summary>
    /// <param name="address">The email address string.</param>
    public static implicit operator EmailAddress(string address) => new(address);
}
