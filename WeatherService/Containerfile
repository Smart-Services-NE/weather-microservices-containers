# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy all project files
COPY ["Contracts/WeatherService.Contracts.csproj", "Contracts/"]
COPY ["Engines/WeatherService.Engines.csproj", "Engines/"]
COPY ["Accessors/WeatherService.Accessors.csproj", "Accessors/"]
COPY ["Utilities/WeatherService.Utilities.csproj", "Utilities/"]
COPY ["Managers/WeatherService.Managers.csproj", "Managers/"]
COPY ["Clients/WebApi/WeatherService.Api.csproj", "Clients/WebApi/"]

# Restore dependencies
RUN dotnet restore "Clients/WebApi/WeatherService.Api.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/Clients/WebApi"
RUN dotnet build "WeatherService.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "WeatherService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WeatherService.Api.dll"]
