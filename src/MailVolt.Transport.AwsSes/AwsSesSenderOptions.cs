namespace MailVolt.Transport.AwsSes;

/// <summary>
/// Options for configuring the AWS SES transport.
/// </summary>
public sealed class AwsSesSenderOptions
{
    /// <summary>
    /// The default configuration section name.
    /// </summary>
    public const string SectionName = "MailVolt:AwsSes";

    /// <summary>
    /// The AWS access key ID.
    /// </summary>
    public required string AccessKeyId { get; set; }

    /// <summary>
    /// The AWS secret access key.
    /// </summary>
    public required string SecretAccessKey { get; set; }

    /// <summary>
    /// The AWS region (e.g. "us-east-1").
    /// </summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// Optional SES configuration set name.
    /// </summary>
    public string? ConfigurationSetName { get; set; }
}
