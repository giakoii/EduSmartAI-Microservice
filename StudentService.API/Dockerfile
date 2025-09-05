FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5051
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["StudentService.API/StudentService.API.csproj", "StudentService.API/"]
COPY ["Shared.API/Shared.API.csproj", "Shared.API/"]
COPY ["Shared.Application/Shared.Application.csproj", "Shared.Application/"]
COPY ["Shared.Common/Shared.Common.csproj", "Shared.Common/"]
COPY ["Shared.Infrastructure/Shared.Infrastructure.csproj", "Shared.Infrastructure/"]
COPY ["StudentService.Application/StudentService.Application.csproj", "StudentService.Application/"]
COPY ["BuildingBlocks.Messaging/BuildingBlocks.Messaging.csproj", "BuildingBlocks.Messaging/"]
COPY ["BuildingBlocks/BuildingBlocks.csproj", "BuildingBlocks/"]
COPY ["StudentService.Domain/StudentService.Domain.csproj", "StudentService.Domain/"]
COPY ["Shared.Domain/Shared.Domain.csproj", "Shared.Domain/"]
COPY ["StudentService.Infrastructure/StudentService.Infrastructure.csproj", "StudentService.Infrastructure/"]
COPY ["UtilityService.Application/UtilityService.Application.csproj", "UtilityService.Application/"]
COPY ["UtilityService.Infrastructure/UtilityService.Infrastructure.csproj", "UtilityService.Infrastructure/"]
COPY ["UtilityService.Domain/UtilityService.Domain.csproj", "UtilityService.Domain/"]
RUN dotnet restore "StudentService.API/StudentService.API.csproj"
COPY . .
WORKDIR "/src/StudentService.API"
RUN dotnet build "./StudentService.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./StudentService.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StudentService.API.dll"]
