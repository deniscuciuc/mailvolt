namespace MailVolt.AutoConfigure;

public sealed class MailVoltAutoOptions
{
    public const string SectionName = "MailVolt";
    public MailVoltFromOptions From { get; set; } = new();
    public MailVoltTransport Transport { get; set; } = MailVoltTransport.Smtp;
    public MailVoltTemplateEngine? Templates { get; set; }

    // Transport sub-sections
    public MailVolt.Transport.Smtp.SmtpSenderOptions? Smtp { get; set; }
    public MailVolt.Transport.SendGrid.SendGridSenderOptions? SendGrid { get; set; }
    public MailVolt.Transport.Mailgun.MailgunSenderOptions? Mailgun { get; set; }
    public MailVolt.Transport.Resend.ResendSenderOptions? Resend { get; set; }
    public MailVolt.Transport.Postmark.PostmarkSenderOptions? Postmark { get; set; }
    public MailVolt.Transport.AzureEmail.AzureEmailSenderOptions? Azure { get; set; }
    public MailVolt.Transport.Brevo.BrevoSenderOptions? Brevo { get; set; }
    public MailVolt.Transport.AwsSes.AwsSesSenderOptions? AwsSes { get; set; }
}
