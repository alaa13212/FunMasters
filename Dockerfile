# ── Stage 1: Build ───────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Download Tailwind CSS standalone CLI and daisyUI for layer caching
RUN mkdir -p FunMasters/FunMasters/Tools FunMasters/FunMasters/Styles \
 && curl -fsSLo FunMasters/FunMasters/Tools/tailwindcss \
      "https://github.com/tailwindlabs/tailwindcss/releases/latest/download/tailwindcss-linux-x64" \
 && chmod +x FunMasters/FunMasters/Tools/tailwindcss \
 && curl -fsSLo FunMasters/FunMasters/Styles/daisyui.mjs \
      "https://github.com/saadeghi/daisyui/releases/latest/download/daisyui.mjs" \
 && curl -fsSLo FunMasters/FunMasters/Styles/daisyui-theme.mjs \
      "https://github.com/saadeghi/daisyui/releases/latest/download/daisyui-theme.mjs"

# Copy project files first for layer caching
COPY FunMasters/FunMasters.Shared/FunMasters.Shared.csproj   FunMasters/FunMasters.Shared/
COPY FunMasters/FunMasters.Client/FunMasters.Client.csproj   FunMasters/FunMasters.Client/
COPY FunMasters/FunMasters/FunMasters.csproj                 FunMasters/FunMasters/
COPY FunMasters/FunMasters/Build/                            FunMasters/FunMasters/Build/

RUN dotnet restore FunMasters/FunMasters/FunMasters.csproj

# Copy all source
COPY FunMasters/ FunMasters/

# Publish
RUN dotnet publish FunMasters/FunMasters/FunMasters.csproj \
      -c Release \
      -o /app/publish \
      --no-restore


# ── Stage 2: Runtime ────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "FunMasters.dll"]
