# Liquid Templates

Render email content using the [Liquid](https://shopify.github.io/liquid/) templating language via [Fluid](https://github.com/sebastienros/fluid).

## Installation

```bash
dotnet add package MailVolt.Templates.Liquid
```

## Registration

```csharp
using MailVolt.Core.DependencyInjection;

builder.Services.AddMailVolt()
    .UseSmtpTransport(options => { /* ... */ })
    .UseLiquidTemplates();
```

## Syntax Basics

Liquid uses `{{ }}` for output and `{% %}` for logic.

```liquid
<h1>Welcome, {{ name }}!</h1>
<p>Your account has been created.</p>
```

## Variable Substitution

Properties are accessed by name. Nested properties use dot notation:

```liquid
<p>Hello {{ user.firstName }} {{ user.lastName }},</p>
<p>Your order #{{ order.id }} totals {{ order.total | format: "C" }}.</p>
```

## Filters

Liquid provides built-in filters:

```liquid
<p>{{ product.name | upcase }}</p>
<p>{{ description | truncate: 100 }}</p>
<p>{{ price | format: "F2" }}</p>
<p>{{ created_at | date: "%Y-%m-%d" }}</p>
```

## Loops

```liquid
<h3>Order Items</h3>
<ul>
{% for item in items %}
    <li>{{ item.name }} - {{ item.price | format: "C" }}</li>
{% endfor %}
</ul>
```

## Conditionals

```liquid
{% if user.isPremium %}
    <p>Thank you for being a premium member!</p>
{% elsif user.isTrial %}
    <p>Your trial expires in {{ user.trialDaysLeft }} days.</p>
{% else %}
    <p><a href="{{ subscribeUrl }}">Upgrade now</a></p>
{% endif %}
```

## Using Templates

```csharp
var result = await _builder
    .From("noreply@example.com")
    .To("user@example.com")
    .Subject("Your Invoice")
    .UsingTemplate("Emails/invoice.liquid", new
    {
        customerName = "Alice",
        invoiceNumber = "INV-001",
        items = new[]
        {
            new { name = "Widget", price = 19.99m },
            new { name = "Gadget", price = 29.99m }
        },
        total = 49.98m
    })
    .SendAsync();
```

With raw template string (not a file path):

```csharp
const string template = """
<h1>Hello {{ name }}!</h1>
<p>You have {{ messages }} new messages.</p>
""";

var result = await _builder
    .UsingTemplate(template, new { name = "Alice", messages = 3 })
    .SendAsync();
```
