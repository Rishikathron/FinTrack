# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy all project files first for layer caching
COPY ["FinTrack/FinTrack.csproj", "FinTrack/"]
COPY ["FinTrack.Core/FinTrack.Core.csproj", "FinTrack.Core/"]
COPY ["FinTrack.AI/FinTrack.AI.csproj", "FinTrack.AI/"]
RUN dotnet restore "FinTrack/FinTrack.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/FinTrack"
RUN dotnet publish "FinTrack.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Create AppData directory for JSON storage (app handles empty folder gracefully)
RUN mkdir -p /app/AppData

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "FinTrack.dll"]