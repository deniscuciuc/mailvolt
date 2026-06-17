# Razor Templates

Render email content using ASP.NET Core Razor views (`.cshtml`). Integrates with the ASP.NET Core Razor view engine for full layout, partial, and `@inject` support.

## Installation

```bash
dotnet add package MailVolt.Templates.Razor
```

> **Console / worker projects:** native Razor views are compiled at build time. Change your `.csproj` to use the Razor SDK and enable MVC Razor support:
>
> ```xml
> <Project Sdk="Microsoft.NET.Sdk.Razor">
>   <PropertyGroup>
>     <AddRazorSupportForMvc>true</AddRazorSupportForMvc>
>   </PropertyGroup>
> </Project>
> ```

## Registration

```csharp
using MailVolt.Core.DependencyInjection;

builder.Services.AddMailVolt()
    .UseSmtpTransport(options => { /* ... */ })
    .UseRazorTemplates();
```

Optionally configure the root directory for templates:

```csharp
.UseRazorTemplates(options =>
{
    options.RootDirectory = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates");
})
```

## Creating `.cshtml` Templates

Place template files in your project (or the configured root directory).

**Simple template** — `Emails/Welcome.cshtml`:

```html
@using MyApp.Models
@model WelcomeModel

<!DOCTYPE html>
<html>
<body>
    <h1>Welcome, @Model.Name!</h1>
    <p>Thank you for joining. Confirm your email <a href="@Model.ConfirmUrl">here</a>.</p>
</body>
</html>
```

## Layout Support

Use `_Layout.cshtml` the same way as in ASP.NET Core MVC:

`Emails/_Layout.cshtml`:

```html
<!DOCTYPE html>
<html>
<head>
    <title>@ViewBag.Title</title>
</head>
<body>
    @RenderBody()
    <footer>
        <p>&copy; @DateTime.Now.Year Example Corp</p>
    </footer>
</body>
</html>
```

Your template sets the layout:

```html
@model WelcomeModel
@{
    Layout = "_Layout";
    ViewBag.Title = "Welcome!";
}

<h1>Welcome, @Model.Name!</h1>
```

## Strongly-Typed Models

```csharp
public class OrderConfirmationModel
{
    public string CustomerName { get; set; } = "";
    public int OrderId { get; set; }
    public decimal Total { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}
```

Template `Emails/OrderConfirmation.cshtml`:

```html
@model OrderConfirmationModel

<h1>Order #@Model.OrderId Confirmed</h1>
<p>Thank you, @Model.CustomerName!</p>

<table>
    @foreach (var item in Model.Items)
    {
        <tr>
            <td>@item.Name</td>
            <td>@item.Price:C</td>
        </tr>
    }
</table>

<p><strong>Total: @Model.Total:C</strong></p>
```

## `@inject` Support

Inject services directly into templates:

```html
@using Microsoft.Extensions.Localization
@inject IStringLocalizer<SharedResources> Localizer

<h1>@Localizer["Welcome.Title"]</h1>
<p>@Localizer["Welcome.Body"]</p>
```

## Using Templates

```csharp
var result = await _builder
    .From("noreply@example.com")
    .To("user@example.com")
    .Subject("Order Confirmed")
    .UsingTemplate("Emails/OrderConfirmation.cshtml", new OrderConfirmationModel
    {
        CustomerName = "Alice",
        OrderId = 1234,
        Total = 49.99m,
        Items = [new OrderItem { Name = "Widget", Price = 49.99m }]
    })
    .SendAsync();
```
