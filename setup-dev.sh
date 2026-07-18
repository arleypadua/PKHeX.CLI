#!/usr/bin/env bash

set -euo pipefail

require_command() {
    local command_name="$1"
    local install_hint="$2"

    if ! command -v "$command_name" >/dev/null 2>&1; then
        printf 'Missing required command: %s\n%s\n' "$command_name" "$install_hint" >&2
        exit 1
    fi
}

require_command git "Install Git from https://git-scm.com/downloads."
require_command npm "Install the current Node.js LTS release from https://nodejs.org/."
require_command dotnet "Install the .NET SDK required by global.json from https://dotnet.microsoft.com/download. On macOS, you can run: brew install --cask dotnet-sdk"

required_dotnet_version=$(awk -F'"' '/"version"/ { print $4; exit }' global.json)
if ! resolved_dotnet_version=$(dotnet --version 2>/dev/null); then
    printf 'No installed .NET SDK satisfies global.json (requested %s with its configured roll-forward policy).\nInstalled SDKs:\n' "$required_dotnet_version" >&2
    dotnet --list-sdks >&2
    printf 'Download it from https://dotnet.microsoft.com/download/dotnet/10.0.\n' >&2
    exit 1
fi

printf 'Using .NET SDK %s (global.json requests %s).\n' "$resolved_dotnet_version" "$required_dotnet_version"

printf 'Initializing PKHeX submodules...\n'
git submodule update --init --recursive

printf 'Restoring .NET projects...\n'
dotnet restore PKHeX.CLI.sln

printf 'Installing and building browser assets...\n'
npm ci --prefix src/PKHeX.Web/_js
npm run build --prefix src/PKHeX.Web/_js
npm ci --prefix src/PKHeX.Web/_blog
npm run build --prefix src/PKHeX.Web/_blog

printf '\nSetup complete. Start the web app with:\n'
printf 'dotnet watch run --project src/PKHeX.Web --no-hot-reload\n'
