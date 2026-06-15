# syntax=docker/dockerfile:1.7

ARG BUILD_VERSION=0.0.0-dev

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CoreAlign.sln ./
COPY server/src/CoreAlign.API/CoreAlign.API.csproj server/src/CoreAlign.API/
COPY server/src/CoreAlign.Application/CoreAlign.Application.csproj server/src/CoreAlign.Application/
COPY server/src/CoreAlign.Domain/CoreAlign.Domain.csproj server/src/CoreAlign.Domain/
COPY server/src/CoreAlign.Infrastructure/CoreAlign.Infrastructure.csproj server/src/CoreAlign.Infrastructure/
COPY server/tests/ server/tests/

RUN dotnet restore server/src/CoreAlign.API/CoreAlign.API.csproj

COPY server/ server/

RUN dotnet publish server/src/CoreAlign.API/CoreAlign.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS runtime

ARG BUILD_VERSION
LABEL org.opencontainers.image.source="https://github.com/corealign/corealign" \
      org.opencontainers.image.version="${BUILD_VERSION}" \
      org.opencontainers.image.title="CoreAlign API" \
      org.opencontainers.image.description="CoreAlign multi-tenant Turkish SaaS ERP API"

WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 8080

USER $APP_UID

ENTRYPOINT ["dotnet", "CoreAlign.API.dll"]
