
using MailVolt.Core.Interfaces;

namespace MailVolt.Transport.Mailgun;
/// <summary>
/// Typed <c>HttpClient</c> interface for sending emails via the Mailgun REST API.
/// </summary>
public interface IMailgunSender : ISender;
