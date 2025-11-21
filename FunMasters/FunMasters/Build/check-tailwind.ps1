$ErrorActionPreference = "Stop"

# Configuration
$TAILWIND_BASE_URL = "https://github.com/tailwindlabs/tailwindcss/releases/latest/download"
$DAISYUI_BASE_URL = "https://github.com/saadeghi/daisyui/releases/latest/download"

# Ensure Tools directory exists
if (!(Test-Path Tools)) {
    New-Item -ItemType Directory -Force -Path Tools | Out-Null
}

# If Tailwind already exists, skip
if ( (Test-Path "Tools/tailwindcss.exe") ) {
    Write-Host "Tailwind CLI already installed."
    exit 0
}


# Main
try {
    Write-Host "  >> Installing Tailwind CSS for Windows x64"
    
    curl.exe -sLo "Tools/tailwindcss.exe" "$TAILWIND_BASE_URL/tailwindcss-windows-x64.exe"
    
    Write-Host "  >> Installing daisyUI"
    curl.exe -sLo "Styles/daisyui.mjs" "$DAISYUI_BASE_URL/daisyui.mjs"
    curl.exe -sLo "Styles/daisyui-theme.mjs" "$DAISYUI_BASE_URL/daisyui-theme.mjs"
}
catch {
    Write-Host "  [X] Installation failed"
    exit 1
}