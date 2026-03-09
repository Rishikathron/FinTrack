# ?? Build stage ??
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["FinTrack.Telegram/FinTrack.Telegram.csproj", "FinTrack.Telegram/"]
COPY ["FinTrack/FinTrack.csproj", "FinTrack/"]
COPY ["FinTrack.Core/FinTrack.Core.csproj", "FinTrack.Core/"]
COPY ["FinTrack.AI/FinTrack.AI.csproj", "FinTrack.AI/"]
RUN dotnet restore "FinTrack.Telegram/FinTrack.Telegram.csproj"

COPY . .
WORKDIR "/src/FinTrack.Telegram"
RUN dotnet publish "FinTrack.Telegram.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ?? Runtime stage ??
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Shared AppData volume (app handles empty folder gracefully)
RUN mkdir -p /app/AppData

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "FinTrack.Telegram.dll"]
