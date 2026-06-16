using MailVolt.Core.Models;

namespace MailVolt.Core.Interfaces;

/// <summary>
/// Fluent builder for constructing and optionally sending email messages.
/// </summary>
public interface IEmailBuilder
{
    /// <summary>
    /// Sets the sender address.
    /// </summary>
    /// <param name="address">The sender email address.</param>
    /// <returns>The builder instance for chaining.</returns>
    IEmailBuilder From(EmailAddress address);

    /// <summary>
    /// Adds a primary recipient.
    /// </summary>
    /// <param name="address">The recipient's email address.</param>
    /// <returns>The builder instance for chaining.</returns>
    IEmailBuilder To(EmailAddress address);

    /// <summary>
    /// Adds a carbon-copy recipient.
    /// </summary>
    /// <param name="address">The CC recipient's email address.</param>
    /// <returns>The builder instance for chaining.</returns>
    IEmailBuilder Cc(EmailAddress address);

    /// <summary>
    /// Adds a blind carbon-copy recipient.
    /// </summary>
    /// <param name="address">The BCC recipient's email address.</param>
    /// <returns>The builder instance for chaining.</returns>
    IEmailBuilder Bcc(EmailAddress address);

    /// <summary>
    /// Sets the reply-to address.
    /// </summary>
    /// <param name="address">The reply-to email address.</param>
    /// <returns>The builder instance for chaining.</returns>
    IEmailBuilder ReplyTo(EmailAddress address);

    /// <summary>
    /// Sets the subject line.
    /// </summary>
    /// <param name="subject">The email subject.</param>
    /// <returns>The builder instance for chaining.</returns>
    IEmailBuilder Subject(string subject);

    /// <summary>
    /// Sets the plain-text body.
    /// </summary>
    /// <param name="text">The plain-text content.</param>
    /// <returns>The builder instance for chaining.</returns>
    IEmailBuilder Body(string text);

    /// <summary>
    /// Sets the HTML body.
    /// </summary>
    /// <param name="html">The HTML content.</param>
    /// <returns>The builder instance for chaining.</returns>
    IEmailBuilder HtmlBody(string html);

    /// <summary>
    /// Sets the plain-text body.
    /// </summary>
    /// <param name="text">The plain-text content.</param>
    /// <returns>The builder instance for chaining.</returns>
    IEmailBuilder TextBody(string text);

    /// <summary>
    /// Sets the email priority.
    /// </summary>
    /// <param name="priority">The priority level.</param>
    /// <returns>The builder instance for chaining.</returns>
    IEmailBuilder Priority(EmailPriority priority);

    /// <summary>
    /// Adds a tag or category to the email.
    /// </summary>
    /// <param name="tag">The tag value.</param>
    /// <returns>The builder instance for chaining.</returns>
    IEmailBuilder Tag(string tag);

    /// <summary>
    /// Sets a custom email header.
    /// </summary>
    /// <param name="key">The header name.</param>
    /// <param name="value">The header value.</param>
    /// <returns>The builder instance for chaining.</returns>
    IEmailBuilder Header(string key, string value);

    /// <summary>
    /// Attaches a file to the email using the attachment builder.
    /// </summary>
    /// <param name="configure">A callback that configures the <see cref="IAttachmentBuilder"/>.</param>
    /// <returns>The builder instance for chaining.</returns>
    IEmailBuilder Attach(Action<IAttachmentBuilder> configure);

    /// <summary>
    /// Configures the email to use a template rendered with the specified model data.
    /// </summary>
    /// <typeparam name="TModel">The type of the model data.</typeparam>
    /// <param name="template">The template content.</param>
    /// <param name="model">The model object to bind to the template.</param>
    /// <returns>The builder instance for chaining.</returns>
    IEmailBuilder UsingTemplate<TModel>(string template, TModel model);

    /// <summary>
    /// Builds the <see cref="EmailMessage"/> from the accumulated builder state.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The constructed <see cref="EmailMessage"/>.</returns>
    Task<EmailMessage> BuildAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds and sends the email in a single operation.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A <see cref="EmailResult"/> indicating success or failure.</returns>
    Task<EmailResult> SendAsync(CancellationToken cancellationToken = default);
}
