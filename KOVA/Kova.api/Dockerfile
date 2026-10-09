FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Kova.api/Kova.api.csproj", "Kova.api/"]
COPY ["Kova.Application/Kova.Application.csproj", "Kova.Application/"]
COPY ["Kova.Domain/Kova.Domain.csproj", "Kova.Domain/"]
COPY ["Kova.Infrastructure/Kova.Infrastructure.csproj", "Kova.Infrastructure/"]
RUN dotnet restore "Kova.api/Kova.api.csproj"
COPY . .
WORKDIR "/src/Kova.api"
RUN dotnet build "./Kova.api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Kova.api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Kova.api.dll"]
