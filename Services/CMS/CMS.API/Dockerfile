FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /src

COPY "CMS.APP.sln" "CMS.APP.sln"

COPY "BuildingBlocks/EventBus/EventBus.csproj" "BuildingBlocks/EventBus/EventBus.csproj"
COPY "BuildingBlocks/EventBusRabbitMQ/EventBusRabbitMQ.csproj" "BuildingBlocks/EventBusRabbitMQ/EventBusRabbitMQ.csproj"
COPY "BuildingBlocks/IntegrationEventLogEF/IntegrationEventLogEF.csproj" "BuildingBlocks/IntegrationEventLogEF/IntegrationEventLogEF.csproj"
COPY "Services/CMS/CMS.API/CMS.API.csproj" "Services/CMS/CMS.API/CMS.API.csproj"
COPY "Services/CMS/CMS.FunctionalTests/CMS.FunctionalTests.csproj" "Services/CMS/CMS.FunctionalTests/CMS.FunctionalTests.csproj"
COPY "Services/CMS/CMS.UnitTests/CMS.UnitTests.csproj" "Services/CMS/CMS.UnitTests/CMS.UnitTests.csproj"

COPY "docker-compose.dcproj" "docker-compose.dcproj"

COPY "NuGet.config" "NuGet.config"

RUN dotnet restore "CMS.APP.sln"

COPY . .
WORKDIR /src/Services/CMS/CMS.API
RUN dotnet publish --no-restore -c Release -o /app

FROM build as unittest
WORKDIR /src/Services/CMS/CMS.UnitTests

FROM build as functionaltest
WORKDIR /src/Services/CMS/CMS.FunctionalTests

FROM build AS publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "CMS.API.dll"]