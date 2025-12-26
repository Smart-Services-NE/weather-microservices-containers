# Use the .NET 10 SDK for building the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["WeatherApi.csproj", "."]
RUN dotnet restore "./WeatherApi.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "WeatherApi.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "WeatherApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the .NET 10 Runtime for the final image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WeatherApi.dll"]
