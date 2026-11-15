# Guidelines for AI Agents (MyFinanceTracker)

## Project Overview
- **Stack:** .NET 11, C# 15, Clean Architecture + CQRS (MediatR), Entity Framework Core 11 (SQLite), Google Sheets API, Telegram.Bot, Gemini API.
- **Architecture Pattern:** Dual persistence with Transactional Outbox pattern:
  - Local database (SQLite via EF Core) acts as the primary transactional store.
  - Background synchronization (`OutboxProcessor`, `OutboxSyncBackgroundService`) pushes transactions to Google Sheets.
- **Central Package Management:** Versions are managed centrally in `Directory.Packages.props`.

---

## Code Style & Naming Conventions

### 1. Naming
- **NO `Async` suffix:** Do **NOT** append the `Async` suffix to asynchronous methods in project code (e.g., use `AddRange(...)`, `Get(...)`, `Handle(...)`, not `AddRangeAsync` or `GetAsync`).
  *(Note: standard third-party/BCL calls like `dbContext.SaveChangesAsync(ct)` or `semaphore.WaitAsync(ct)` keep their library names).*
- **NO `_` prefix for private fields:** Private fields must be named in `camelCase` without a leading underscore (e.g., `private readonly IUserRepository userRepository;`, `private static readonly SemaphoreSlim semaphore = new(1, 1);`).
- **Cancellation Tokens:** Always name the cancellation token parameter `ct` (e.g., `CancellationToken ct`).
- **Interfaces:** Prefix with `I` (e.g., `ITransactionRepository`, `IDbContextFactory<SqliteDbContext>`).
- **Test variables:** Always name the subject under test `sut` or specific repository name (e.g., `repository`, `sut`).

### 2. Language & Architecture Conventions
- **Primary Constructors:** Always prefer primary constructors for dependency injection (e.g., `internal sealed partial class SqliteTransactionRepository(IDbContextFactory<SqliteDbContext> dbContextFactory, TimeProvider timeProvider, ...) : ITransactionRepository`).
- **Class Modifiers:** Mark classes as `internal sealed` by default unless public accessibility, partiality (for logging), or inheritance is explicitly needed.
- **Namespaces:** Always use file-scoped namespaces (`namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite;`).
- **Modern C# Idioms:**
  - Collection expressions: use `[...]` when target type is known or declared on the left (e.g., `string[] items = ["a", "b"];`). Do **NOT** use ugly casts like `((Type[])[...])` — in fluent calls or where target typing is ambiguous, keep `new[] { ... }`.
  - Prefer `var` when type is clear from the RHS; when initializing collection expressions with explicit types (e.g., `string[] items = [...]`), explicit typing is preferred over `var items = new[] { ... }`.
  - Target-typed `new()` where the type is clear from the context.
  - Pattern matching switch expressions with exhaustive handling (`_ => throw new UnreachableException()`).
- **KISS & Simplicity:** Avoid premature optimization, complex batching, or extra caching layers when data is already static or in-memory. Keep code clear, direct, and readable.
- **CQRS & Responses:**
  - Handlers implement MediatR's `IRequestHandler<TRequest, TResponse>`.
  - Prefer strongly-typed / discriminated union responses (e.g., `Response.Success`, `Response.ValidationError`).
- **Logging:**
  - Use high-performance source-generated logging with `[LoggerMessage]`.
  - Place partial logging methods in a dedicated companion file named `<ClassName>.Logging.cs`.
  - Event IDs and templates come from `LogEvents`.

### 3. Persistence & EF Core
- **Entity Configurations:** Separate configuration classes implementing `IEntityTypeConfiguration<TEntity>`.
- **DbContext Access:** Prefer `IDbContextFactory<SqliteDbContext>` with `await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);` in repositories and processors.
- **Outbox Pattern:** State-changing operations in SQLite should insert corresponding `OutboxMessageEntity` in the same transaction/scope.

---

## Testing Standards

- **Frameworks:** xUnit, FluentAssertions, NSubstitute.
- **Assertions:**
  - Prefer **FluentAssertions** (`result.Should().Be(...)`, `items.Should().HaveCount(1)`) in test projects where the package is already referenced.
  - If a test project does not reference FluentAssertions (e.g., `Domain.Tests`), use standard xUnit `Assert.*`. Do not add new package dependencies without explicit user request.
- **Database Tests:** Use in-memory SQLite with `SqliteConnection("DataSource=:memory:")`, `context.Database.EnsureCreated()`, and `TestDbContextFactory`.
- **Time Testing:** Inherit from `TimeProvider` (e.g., nested `FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider`) instead of external mocking packages.
- **Test Method Naming:** `Method_Scenario_ExpectedResult` (without `Async` suffix), for example:
  `async Task AddRange_ValidTransactions_SavesToDatabaseAndOutbox()`
- **Structure:** Follow Arrange-Act-Assert with comments:
  ```csharp
  // arrange
  ...
  // act
  ...
  // assert
  ...
  ```
- **Mocking:** Use NSubstitute (`Substitute.For<T>()`, `Arg.Any<CancellationToken>()`).

---

## Known Architectural Trade-offs (By Design — Do NOT "Fix")

Do **NOT** flag the following behaviors as bugs or propose refactoring them unless explicitly asked by the user:

1. **Google Sheets Purpose & Usage Model:**
   - Google Sheets is strictly an external flat-data mirror/backup consumed directly by AI agents (e.g., via direct spreadsheet integrations) and used for emergency manual inspection.
   - No Excel-like formulas, pivot tables, charts, or cell formatting are used for reporting (`ValueInputOption.RAW` is intentional).
   - Humans do **not** edit the spreadsheet during normal operation; manual edits in the Google Sheets UI happen only during emergency recovery (e.g., manually fixing/deleting a corrupted row after a Dead Letter Queue alert).
2. **Google Sheets Write Symmetry (`AppendRows`) vs. Read Fail-Fast (`LoadValidRows`):**
   - Both `GoogleSheetsTransactionRepository.AddRange` and `GoogleSheetsCategoryRepository.Add` intentionally append rows directly via `AppendRows` in a single round-trip **without** pre-fetching the sheet to check for duplicate IDs or sheet consistency. Primary key uniqueness and domain invariants are enforced by SQLite before Outbox messages are created.
   - Full-sheet reads (`LoadValidRows`) are performed **only** when technically required by Google Sheets API (`GetAll`, and resolving row indices for `Update` and `DeleteRange`).
   - When `LoadValidRows` runs, it strictly **fails fast** (`GoogleSheetsPersistenceException`) on duplicate IDs or malformed rows instead of silently deduplicating (`DistinctBy`) or skipping invalid rows, surfacing data corruption via Dead Letter Queue notifications.
3. **One-Way Sync & Cold-Start Bootstrap Only:**
   - `SqliteDatabaseInitializerHostedService` pulls data from Google Sheets **only** when the local SQLite database is completely empty (`ShouldSkipInitialSync`). Once initialized, SQLite is the single source of truth, and runtime synchronization is strictly one-way (`SQLite -> Outbox -> Google Sheets`).
4. **Strict FIFO Outbox Ordering (Head-of-Line Blocking):**
   - `OutboxProcessor` processes messages sequentially by `Id` and stops the batch (`break`) on the first transient failure (until `MaxRetryAttempts` moves the message to `DeadLetterMessages`). This preserves causal ordering (e.g., `AddCategory` before `AddTransaction`, `Add` before `Update`/`Delete`).
5. **Single-User & Stateless Single-Turn AI Processing:**
   - The application is a single-user bot (`AllowedUserId`); multi-tenancy (`UserId` columns) is intentionally omitted.
   - `GeminiInputProcessor` is stateless (no conversation history) and executes tool calls in a single turn without sending tool outputs back to Gemini (responses are formatted deterministically by `TelegramResponseFormatter`). Multiple tool calls in a single turn execute sequentially without a cross-request database transaction.
6. **Wall-Clock Local `DateTime` for Transactions:**
   - `Transaction.Date` intentionally uses `DateTime` representing user local time (`timeProvider.GetLocalNow().DateTime`, serialized as `yyyy-MM-dd HH:mm:ss` in Sheets), while system audit timestamps (`CreatedAtUtc`, `FailedAtUtc`) use UTC `DateTimeOffset`. Do not convert `Transaction.Date` to UTC or `DateTimeOffset`.

---

## Workflow & Agent Rules

### 1. Critical Thinking & Architectural Review
- **No Sycophancy:** Never agree with suggestions just to be polite. If an idea or approach is flawed, state it directly.
- **Role During Design & Planning:** Act as a pragmatic, critically minded tech lead.
- **Challenge Bad Patterns:** If a proposed design involves hacks/workarounds, over-engineering, hidden edge cases/bugs, or violates KISS/YAGNI, explicitly call out the risks and trade-offs.
- **Constructive Alternatives:** Always propose a simpler, cleaner, or more idiomatic alternative aligned with .NET and Clean Architecture principles.
- **User's Final Call:** If the user acknowledges the risks and explicitly chooses a specific trade-off, proceed with implementation without unnecessary pushback.

### 2. Two-Step Development Workflow
1. **Implementation first:** Implement the feature or changes first and present the solution to the user for review.
2. **Tests after approval:** Write unit and integration tests only **after** the implementation has been reviewed and approved by the user.

### 3. Git Discipline & Approvals
- **No automatic commits:** Never commit changes automatically without explicit user instruction.
- **Review before commit:** Before creating any commit, stop, summarize the changes, and confirm with the user.

### 4. Verification & Code Quality
- **Verification:** Run `dotnet build` and `dotnet test` to verify changes before reporting completion.
- **Complete Implementations:** Never leave incomplete code, `// TODO: implement later`, or placeholder `throw new NotImplementedException()`.
- **Preserve Documentation:** Do not delete existing comments or docstrings unless directly asked.
