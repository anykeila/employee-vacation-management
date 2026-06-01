# syntax=docker/dockerfile:1

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore as a separate, cacheable layer: copy only project files first.
COPY global.json ./
COPY src/VacationManagement.Domain/VacationManagement.Domain.csproj src/VacationManagement.Domain/
COPY src/VacationManagement.Application/VacationManagement.Application.csproj src/VacationManagement.Application/
COPY src/VacationManagement.Infrastructure/VacationManagement.Infrastructure.csproj src/VacationManagement.Infrastructure/
COPY src/VacationManagement.Api/VacationManagement.Api.csproj src/VacationManagement.Api/
RUN dotnet restore src/VacationManagement.Api/VacationManagement.Api.csproj

# Copy the remaining source and publish.
COPY . .
RUN dotnet publish src/VacationManagement.Api/VacationManagement.Api.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# curl is used by the container HEALTHCHECK to probe /health/ready.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Run as the non-root user provided by the .NET base image (security baseline).
USER $APP_UID

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "VacationManagement.Api.dll"]
