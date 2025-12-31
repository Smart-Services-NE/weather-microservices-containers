FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["NotificationService/Clients/WebApi/NotificationService.Api.csproj", "NotificationService/Clients/WebApi/"]
COPY ["NotificationService/Contracts/NotificationService.Contracts.csproj", "NotificationService/Contracts/"]
COPY ["NotificationService/Managers/NotificationService.Managers.csproj", "NotificationService/Managers/"]
COPY ["NotificationService/Engines/NotificationService.Engines.csproj", "NotificationService/Engines/"]
COPY ["NotificationService/Accessors/NotificationService.Accessors.csproj", "NotificationService/Accessors/"]
COPY ["NotificationService/Utilities/NotificationService.Utilities.csproj", "NotificationService/Utilities/"]

RUN dotnet restore "NotificationService/Clients/WebApi/NotificationService.Api.csproj"

COPY NotificationService/ NotificationService/

WORKDIR "/src/NotificationService/Clients/WebApi"
RUN dotnet build "NotificationService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "NotificationService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NotificationService.Api.dll"]
