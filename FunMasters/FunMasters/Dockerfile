FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["FunMasters/FunMasters/FunMasters.csproj", "FunMasters/FunMasters/"]
COPY ["FunMasters/FunMasters.Client/FunMasters.Client.csproj", "FunMasters/FunMasters.Client/"]
RUN dotnet restore "FunMasters/FunMasters/FunMasters.csproj"
COPY . .
WORKDIR "/src/FunMasters/FunMasters"
RUN dotnet build "./FunMasters.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./FunMasters.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FunMasters.dll"]
