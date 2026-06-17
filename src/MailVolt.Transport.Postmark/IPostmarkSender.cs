using MailVolt.Core.Interfaces;

namespace MailVolt.Transport.Postmark;

/// <summary>
/// Postmark-specific sender interface. Exposes the same contract as <see cref="ISender"/>
/// so that consumers can depend on the transport directly when needed.
/// </summary>
public interface IPostmarkSender : ISender;
