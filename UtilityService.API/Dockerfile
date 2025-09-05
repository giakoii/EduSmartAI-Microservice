FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["UtilityService.API/UtilityService.API.csproj", "UtilityService.API/"]
COPY ["UtilityService.Application/UtilityService.Application.csproj", "UtilityService.Application/"]
COPY ["Shared.Application/Shared.Application.csproj", "Shared.Application/"]
COPY ["Shared.Common/Shared.Common.csproj", "Shared.Common/"]
COPY ["BuildingBlocks/BuildingBlocks.csproj", "BuildingBlocks/"]
COPY ["BuildingBlocks.Messaging/BuildingBlocks.Messaging.csproj", "BuildingBlocks.Messaging/"]
COPY ["Shared.API/Shared.API.csproj", "Shared.API/"]
COPY ["Shared.Infrastructure/Shared.Infrastructure.csproj", "Shared.Infrastructure/"]
COPY ["UtilityService.Infrastructure/UtilityService.Infrastructure.csproj", "UtilityService.Infrastructure/"]
COPY ["UtilityService.Domain/UtilityService.Domain.csproj", "UtilityService.Domain/"]
COPY ["Shared.Domain/Shared.Domain.csproj", "Shared.Domain/"]
RUN dotnet restore "UtilityService.API/UtilityService.API.csproj"
COPY . .
WORKDIR "/src/UtilityService.API"
RUN dotnet build "./UtilityService.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./UtilityService.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UtilityService.API.dll"]
