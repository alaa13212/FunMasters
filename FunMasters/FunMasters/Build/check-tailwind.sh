#!/usr/bin/env bash

# This script installs Tailwind CSS Standalone file and daisyUI.
# - Downloads the latest Tailwind CSS binary for the detected OS and architecture.
# - Downloads the daisyUI bundle file.

set -euo pipefail

if [ -f Tools/tailwindcss ]; then
    echo "Tailwind CLI already installed."
    exit 0
fi

echo "Tailwind CLI not found. Downloading..."

# Configuration
TAILWIND_BASE_URL="https://github.com/tailwindlabs/tailwindcss/releases/latest/download"
DAISYUI_BASE_URL="https://github.com/saadeghi/daisyui/releases/latest/download"

# Error handler
trap 'echo "  ❌ Installation failed" >&2; exit 1' ERR

get_os() {
    case "$(uname -s)" in
        Linux*) echo "linux";;
        Darwin*) echo "macos";;
        *) echo "unknown";;
    esac
}

get_arch() {
    case "$(uname -m)" in
        x86_64|amd64) echo "x64";;
        aarch64|arm64) echo "arm64";;
        *) echo "unknown";;
    esac
}

get_musl_suffix() {
    [ "$(uname -s)" = "Linux" ] && ldd --version 2>&1 | grep -q musl && echo "-musl" || echo ""
}

format_os() {
    case "$1" in
        linux) echo "Linux";;
        macos) echo "macOS";;
        *) echo "$1";;
    esac
}

# Main
main() {
    local os=$(get_os)
    local arch=$(get_arch)

    [ "$os" = "unknown" ] || [ "$arch" = "unknown" ] && echo "❌ Unsupported system" >&2 && exit 1

    mkdir -p Tools

    local filename="tailwindcss-$os-$arch$(get_musl_suffix)"
    echo "  🚚 Installing Tailwind CSS for $(format_os "$os") $arch"
    curl -fsSLo Tools/tailwindcss "$TAILWIND_BASE_URL/$filename"
    chmod +x Tools/tailwindcss

    echo "  🚚 Installing daisyUI"
    curl -fsSLo Styles/daisyui.mjs "$DAISYUI_BASE_URL/daisyui.mjs"
    curl -fsSLo Styles/daisyui-theme.mjs "$DAISYUI_BASE_URL/daisyui-theme.mjs"
}

main

echo "Tailwind CLI downloaded."
