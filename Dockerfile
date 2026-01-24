# Stage 1: Build (.NET 10 SDK)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY . .

RUN dotnet restore MyFinanceTracker.sln

RUN dotnet publish src/MyFinanceTracker.Host/MyFinanceTracker.Host.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Stage 2: Runtime (.NET 10 Runtime)
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app

COPY --from=build /app/publish .

RUN mkdir -p /app/secrets

ENTRYPOINT ["dotnet", "MyFinanceTracker.Host.dll"]