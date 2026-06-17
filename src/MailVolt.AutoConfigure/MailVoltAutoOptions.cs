namespace MailVolt.AutoConfigure;

public sealed class MailVoltAutoOptions
{
    public const string SectionName = "MailVolt";
    public MailVoltFromOptions From { get; set; } = new();
    public MailVoltTransport Transport { get; set; } = MailVoltTransport.Smtp;
    public MailVoltTemplateEngine? Templates { get; set; }

    public Transport.Smtp.SmtpSenderOptions? Smtp { get; set; }
    public Transport.SendGrid.SendGridSenderOptions? SendGrid { get; set; }
    public Transport.Mailgun.MailgunSenderOptions? Mailgun { get; set; }
    public Transport.Resend.ResendSenderOptions? Resend { get; set; }
    public Transport.Postmark.PostmarkSenderOptions? Postmark { get; set; }
    public Transport.AzureEmail.AzureEmailSenderOptions? Azure { get; set; }
    public Transport.Brevo.BrevoSenderOptions? Brevo { get; set; }
    public Transport.AwsSes.AwsSesSenderOptions? AwsSes { get; set; }
}
