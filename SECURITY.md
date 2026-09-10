# Security Policy

## Supported Versions

| Version   | Supported          |
|-----------|--------------------|
| 0.1.x     | :white_check_mark: |
| < 0.1.0   | :x:                |

Only the latest patch of the latest minor release receives security fixes. All MailVolt
packages are versioned and released together, so a fix ships as a new patch across all of
them.

## Reporting a Vulnerability

**Please do not open a public issue for a security vulnerability.**

Report it through GitHub's private vulnerability reporting, which is the preferred channel:

<https://github.com/deniscuciuc/mailvolt/security/advisories/new>

If you cannot use GitHub, email **denis.cuciuc@zelqonworks.com** instead.

Please include:

- A description of the vulnerability and its impact
- Steps to reproduce, or a proof of concept
- The affected version(s) and transport(s)
- Any suggested mitigation

## What to Expect

| Stage | Target |
|---|---|
| Acknowledgement of your report | Within 48 hours |
| Initial assessment and severity triage | Within 5 working days |
| Fix released for a high or critical issue | Within 30 days of triage |
| Fix released for a moderate or low issue | Next scheduled release |

If you have not heard back within 48 hours, please follow up — an unanswered report usually
means it did not arrive.

## Responsible Disclosure

Please give us a reasonable opportunity to release a fix before disclosing publicly. We will
work with you to agree a disclosure timeline, credit you in the advisory and the changelog
unless you prefer otherwise, and publish a GitHub Security Advisory when the fix ships.

## Scope

In scope: the MailVolt packages in this repository, including credential handling, the
handling of untrusted template input, and anything that could cause mail to be sent to an
unintended recipient.

Out of scope: vulnerabilities in the email providers themselves or in their SDKs — report
those to the provider, though we would still like to know so we can pin or work around an
affected version. Also out of scope: the example projects under `examples/`, which exist to
illustrate usage and are not published.

## Dependency Advisories

The build does not suppress NuGet vulnerability warnings: `NU1902` and `NU1903` are errors,
so a dependency with a published advisory fails CI. Dependabot is enabled for NuGet and
GitHub Actions, and CodeQL runs on every push to `main` and weekly.
