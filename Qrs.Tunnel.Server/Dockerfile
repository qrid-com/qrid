# syntax=docker/dockerfile:1
#
# Standard production image for Qrs.Tunnel.Server, suited for long-running
# hosts (e.g. an Amazon EC2 Linux VM) where cold-start latency is not a
# primary concern.
#
# For Google Cloud Run or other Knative-compatible serverless platforms, use
# Dockerfile.gcr instead, which adds cold-start tuning and the
# ASPNETCORE_HTTP_PORTS/$PORT wiring required by the Cloud Run container
# contract (https://docs.cloud.google.com/run/docs/container-contract).

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
# The app binds to 4999 (see appsettings.json "Urls": "http://+:4999/"), not the
# ASP.NET Core defaults of 80/443, so expose the actual listening port.
EXPOSE 4999

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Project references span multiple folders, so the full source tree is needed for restore.
COPY . .
WORKDIR /src/Qrs.Tunnel.Server
RUN dotnet restore "./Qrs.Tunnel.Server.csproj"

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Qrs.Tunnel.Server.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Qrs.Tunnel.Server.dll"]
