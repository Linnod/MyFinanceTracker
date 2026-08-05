# Stage 1: Build & Test
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY ["MyFinanceTracker.slnx", "./"]
COPY ["src/MyFinanceTracker.Host/MyFinanceTracker.Host.csproj", "src/MyFinanceTracker.Host/"]
COPY ["src/MyFinanceTracker.Domain/MyFinanceTracker.Domain.csproj", "src/MyFinanceTracker.Domain/"]
COPY ["src/MyFinanceTracker.Common/MyFinanceTracker.Common.csproj", "src/MyFinanceTracker.Common/"]
COPY ["src/MyFinanceTracker.UseCases/MyFinanceTracker.UseCases.csproj", "src/MyFinanceTracker.UseCases/"]
COPY ["src/MyFinanceTracker.Interactions.Telegram/MyFinanceTracker.Interactions.Telegram.csproj", "src/MyFinanceTracker.Interactions.Telegram/"]
COPY ["src/MyFinanceTracker.Interactions.Console/MyFinanceTracker.Interactions.Console.csproj", "src/MyFinanceTracker.Interactions.Console/"]
COPY ["src/MyFinanceTracker.Interactions.Api/MyFinanceTracker.Interactions.Api.csproj", "src/MyFinanceTracker.Interactions.Api/"]
COPY ["src/MyFinanceTracker.Infrastructure.Persistence/MyFinanceTracker.Infrastructure.Persistence.csproj", "src/MyFinanceTracker.Infrastructure.Persistence/"]
COPY ["src/MyFinanceTracker.Infrastructure.Persistence.Yaml/MyFinanceTracker.Infrastructure.Persistence.Yaml.csproj", "src/MyFinanceTracker.Infrastructure.Persistence.Yaml/"]
COPY ["src/MyFinanceTracker.Infrastructure.Persistence.GoogleSheets/MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.csproj", "src/MyFinanceTracker.Infrastructure.Persistence.GoogleSheets/"]
COPY ["src/MyFinanceTracker.CommandProcessing.Text/MyFinanceTracker.CommandProcessing.Text.csproj", "src/MyFinanceTracker.CommandProcessing.Text/"]

COPY ["tests/MyFinanceTracker.Infrastructure.Persistence.Yaml.Tests/MyFinanceTracker.Infrastructure.Persistence.Yaml.Tests.csproj", "tests/MyFinanceTracker.Infrastructure.Persistence.Yaml.Tests/"]
COPY ["tests/MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Tests/MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Tests.csproj", "tests/MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Tests/"]
COPY ["tests/MyFinanceTracker.Domain.Tests/MyFinanceTracker.Domain.Tests.csproj", "tests/MyFinanceTracker.Domain.Tests/"]
COPY ["tests/MyFinanceTracker.CommandProcessing.Text.Tests/MyFinanceTracker.CommandProcessing.Text.Tests.csproj", "tests/MyFinanceTracker.CommandProcessing.Text.Tests/"]

COPY ["Directory.Packages.props", "./"]

RUN dotnet restore

COPY . .

WORKDIR "/app/src/MyFinanceTracker.Host"
RUN dotnet publish "MyFinanceTracker.Host.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /app/publish .

ENV DOTNET_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "MyFinanceTracker.Host.dll"]