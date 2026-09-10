namespace MailVolt.Core.Models;

/// <summary>
/// Represents an email address with an optional display name.
/// </summary>
/// <param name="Address">The email address (e.g. user@example.com).</param>
/// <param name="DisplayName">Optional display name shown alongside the address.</param>
public sealed record EmailAddress(string Address, string? DisplayName = null)
{
    /// <summary>
    /// The email address (e.g. user@example.com).
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the address is null, blank, or not a valid address.</exception>
    public string Address { get; init; } = Validate(Address);

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

    /// <summary>
    /// Creates an <see cref="EmailAddress"/> from a string.
    /// </summary>
    /// <param name="address">The email address string.</param>
    /// <returns>The parsed address.</returns>
    public static EmailAddress FromString(string address) => new(address);

    /// <summary>
    /// Determines whether <paramref name="address"/> is a well-formed email address.
    /// </summary>
    /// <param name="address">The candidate address.</param>
    /// <returns><see langword="true"/> when the address is well-formed.</returns>
    public static bool IsValid(string? address) => TryNormalize(address, out _);

    /// <summary>
    /// Attempts to create an <see cref="EmailAddress"/> without throwing.
    /// </summary>
    /// <param name="address">The candidate address.</param>
    /// <param name="result">The parsed address, or <see langword="null"/> when invalid.</param>
    /// <returns><see langword="true"/> when the address was parsed.</returns>
    public static bool TryParse(string? address, out EmailAddress? result)
    {
        if (!TryNormalize(address, out var normalized))
        {
            result = null;
            return false;
        }

        result = new EmailAddress(normalized);
        return true;
    }

    private static readonly System.Buffers.SearchValues<char> InvalidCharacters =
        System.Buffers.SearchValues.Create(" \t\r\n,;<>\"");

    private static string Validate(string address) =>
        TryNormalize(address, out var normalized)
            ? normalized
            : throw new ArgumentException(
                $"'{address}' is not a valid email address.", nameof(address));

    /// <remarks>
    /// Deliberately a structural check rather than full RFC 5322 parsing: the goal is to
    /// reject the mistakes that would otherwise surface as an opaque provider API error
    /// (no @, empty local or domain part, whitespace, a domain with no dot), not to
    /// second-guess the receiving mail server.
    /// </remarks>
    private static bool TryNormalize(string? address, out string normalized)
    {
        normalized = string.Empty;

        if (string.IsNullOrWhiteSpace(address))
        {
            return false;
        }

        var trimmed = address.Trim();

        if (trimmed.AsSpan().IndexOfAny(InvalidCharacters) >= 0)
        {
            return false;
        }

        var at = trimmed.LastIndexOf('@');
        if (at <= 0 || at == trimmed.Length - 1)
        {
            return false;
        }

        var domain = trimmed.AsSpan(at + 1);
        if (!domain.Contains('.') || domain.StartsWith(".") || domain.EndsWith("."))
        {
            return false;
        }

        normalized = trimmed;
        return true;
    }
}
