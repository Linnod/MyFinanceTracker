using Google.GenAI;
using Google.GenAI.Types;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using MyFinanceTracker.Infrastructure.Persistence;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Clients;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Data;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Initialization;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Sync;
using MyFinanceTracker.InputProcessing;
using MyFinanceTracker.InputProcessing.Gemini;
using MyFinanceTracker.UseCases;

namespace MyFinanceTracker.Integration.Tests.Fixtures;

internal sealed class IntegrationTestFixture : IAsyncDisposable
{
    private static readonly DateTimeOffset DefaultUtcNow = new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);

    private readonly SqliteConnection connection;
    private readonly ServiceProvider serviceProvider;

    public IntegrationTestFixture(DateTimeOffset? utcNow = null)
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        SheetsClient = new InMemoryGoogleSheetsClient();
        GeminiHandler = new StubGeminiHttpHandler();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GoogleSheets:SpreadsheetId"] = "test-spreadsheet-id",
                ["GoogleSheets:CredentialsJson"] = "{}",
                ["GoogleSheets:ApplicationName"] = "MyFinanceTrackerTests",
                ["GoogleSheets:TransactionsSheetName"] = InMemoryGoogleSheetsClient.TransactionsSheetName,
                ["GoogleSheets:CategoriesSheetName"] = InMemoryGoogleSheetsClient.CategoriesSheetName,
                ["SqlitePersistence:ConnectionString"] = "Data Source=:memory:",
                ["SqlitePersistence:SyncIntervalSeconds"] = "60",
                ["SqlitePersistence:MaxRetryAttempts"] = "5",
                ["Gemini:ApiKey"] = "test-gemini-api-key",
                ["Gemini:Model"] = "gemini-2.5-flash"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<TimeProvider>(new FixedTimeProvider(utcNow ?? DefaultUtcNow));
        services
            .AddGeminiInputProcessor(configuration)
            .AddUseCases()
            .AddPersistence(configuration);

        services.RemoveAll<IGoogleSheetsClient>();
        services.AddSingleton<IGoogleSheetsClient>(SheetsClient);

        services.RemoveAll<Client>();
        services.AddSingleton(_ => new Client(
            apiKey: "test-gemini-api-key",
            enterprise: false,
            clientOptions: new ClientOptions
            {
                HttpClientFactory = () => new HttpClient(GeminiHandler, disposeHandler: false)
            }));

        services.RemoveAll<IDbContextFactory<SqliteDbContext>>();
        services.RemoveAll<DbContextOptions<SqliteDbContext>>();
        services.RemoveAll<DbContextOptions>();
        services.RemoveAll<SqliteDbContext>();
        services.AddDbContextFactory<SqliteDbContext>(builder => builder.UseSqlite(connection));

        serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
    }

    public InMemoryGoogleSheetsClient SheetsClient { get; }

    public StubGeminiHttpHandler GeminiHandler { get; }

    public OutboxProcessor OutboxProcessor => serviceProvider.GetRequiredService<OutboxProcessor>();

    public SqliteDatabaseInitializerHostedService DatabaseInitializer =>
        serviceProvider.GetServices<IHostedService>()
            .OfType<SqliteDatabaseInitializerHostedService>()
            .Single();

    public Task InitializeDatabase(CancellationToken ct = default) =>
        DatabaseInitializer.StartAsync(ct);

    public async Task<ProcessingResult> SendInput(Input input, CancellationToken ct = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        return await sender.Send(input, ct);
    }

    public Task<SqliteDbContext> CreateDbContext(CancellationToken ct = default) =>
        serviceProvider.GetRequiredService<IDbContextFactory<SqliteDbContext>>().CreateDbContextAsync(ct);

    public async ValueTask DisposeAsync()
    {
        await serviceProvider.DisposeAsync();
        GeminiHandler.Dispose();
        await connection.DisposeAsync();
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
