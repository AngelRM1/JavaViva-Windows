#!/bin/bash
# Recompila JavaViva para Windows x64.
# En Windows: ejecuta con .NET 8 SDK instalado.
# En Linux: necesita el SDK en ~/dotnet o en el PATH.
set -e
export DOTNET_CLI_TELEMETRY_OPTOUT=1
DOTNET="${DOTNET:-$HOME/dotnet/dotnet}"
if ! command -v dotnet >/dev/null 2>&1 && [ -x "$DOTNET" ]; then export PATH="$HOME/dotnet:$PATH"; fi
cd "$(dirname "$0")"
dotnet publish -r win-x64 -c Release -o ../publish
ls -lh ../publish/JavaViva.exe
file ../publish/JavaViva.exe
