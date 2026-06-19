namespace ConsoleSmtpRazor;

public record WelcomeModel(
    string Name,
    string Plan,
    DateTimeOffset SentAt);
