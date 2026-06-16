using MailVolt.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Sample.AspNetCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WelcomeController(IEmailBuilder emailBuilder) : ControllerBase
{
    [HttpPost("{email}")]
    public async Task<IActionResult> SendWelcome(string email)
    {
        var result = await emailBuilder
            .From(new MailVolt.Core.Models.EmailAddress("welcome@example.com", "MailVolt Team"))
            .To(email)
            .Subject("Welcome to MailVolt!")
            .HtmlBody("<h1>Welcome!</h1><p>Thank you for choosing MailVolt.</p>")
            .SendAsync();

        if (result.IsSuccess)
            return Ok(new { messageId = result.MessageId });

        return Problem(detail: result.Error, statusCode: 500);
    }
}
