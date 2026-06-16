# Handlebars Templates

Render email content using the [Handlebars.Net](https://github.com/Handlebars-Net/Handlebars.Net) templating engine.

## Installation

```bash
dotnet add package MailVolt.Templates.Handlebars
```

## Registration

```csharp
using MailVolt.Core.DependencyInjection;

builder.Services.AddMailVolt()
    .UseSmtpTransport(options => { /* ... */ })
    .UseHandlebarsTemplates();
```

## Syntax Basics

Handlebars uses `{{ }}` for expressions. It is logic-less by design — helpers provide the logic.

```handlebars
<h1>Welcome, {{ name }}!</h1>
<p>Your account has been created.</p>
```

## Variable Substitution

```handlebars
<p>Hello {{ user.firstName }} {{ user.lastName }},</p>
<p>Your order #{{ order.id }} totals {{ order.total }}.</p>
```

## Conditionals

```handlebars
{{#if isPremium}}
    <p>Thank you for being a premium member!</p>
{{else}}
    <p><a href="{{ subscribeUrl }}">Upgrade now</a></p>
{{/if}}
```

## Loops

```handlebars
<h3>Order Items</h3>
<ul>
{{#each items}}
    <li>{{name}} - {{price}}</li>
{{/each}}
</ul>
```

Access the current index with `@index`:

```handlebars
{{#each items}}
    <tr>
        <td>{{ @index }}</td>
        <td>{{ name }}</td>
        <td>{{ price }}</td>
    </tr>
{{/each}}
```

## Helpers

Register custom helpers during configuration:

```csharp
.UseHandlebarsTemplates(options =>
{
    options.RegisterHelper("formatDate", (writer, context, parameters) =>
    {
        var date = DateTime.Parse(parameters[0].ToString()!);
        writer.WriteSafeString(date.ToString("MMMM dd, yyyy"));
    });

    options.RegisterHelper("uppercase", (writer, context, parameters) =>
    {
        writer.WriteSafeString(parameters[0].ToString()!.ToUpperInvariant());
    });
})
```

Usage in templates:

```handlebars
<p>Sent on {{ formatDate sentDate }}</p>
<p>{{ uppercase title }}</p>
```

## Partials

Partials allow template reuse:

`_header.hbs`:

```handlebars
<h1>{{ title }}</h1>
```

Main template:

```handlebars
{{> header title="Welcome" }}
<p>Body content here.</p>
```

Register partials at startup:

```csharp
.UseHandlebarsTemplates(options =>
{
    options.RegisterPartial("footer", "<p>&copy; {{year}} Example Corp</p>");
})
```

## Using Templates

```csharp
var result = await _builder
    .From("noreply@example.com")
    .To("user@example.com")
    .Subject("Your Receipt")
    .UsingTemplate("Emails/receipt.hbs", new
    {
        customerName = "Alice",
        items = new[]
        {
            new { name = "Widget", price = "$19.99" },
            new { name = "Gadget", price = "$29.99" }
        }
    })
    .SendAsync();
```
