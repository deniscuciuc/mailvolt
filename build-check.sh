#!/usr/bin/env bash
# Build check for MailVolt.Transport.Resend
set -euo pipefail
cd "$(dirname "$0")"
dotnet build src/MailVolt.Transport.Resend/MailVolt.Transport.Resend.csproj
