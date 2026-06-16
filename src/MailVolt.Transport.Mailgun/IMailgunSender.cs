namespace MailVolt.Transport.Mailgun;

using MailVolt.Core.Interfaces;

/// <summary>
/// Typed <c>HttpClient</c> interface for sending emails via the Mailgun REST API.
/// </summary>
public interface IMailgunSender : ISender;
