using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Entities;
using MyFinanceTracker.InputProcessing;
using MyFinanceTracker.Integration.Tests.Fixtures;
using MyFinanceTracker.UseCases.Category.Create;
using MyFinanceTracker.UseCases.Category.List;
using MyFinanceTracker.UseCases.Category.Update;
using MyFinanceTracker.UseCases.Transaction.Create;
using MyFinanceTracker.UseCases.Transaction.Delete;
using MyFinanceTracker.UseCases.Transaction.Get;
using MyFinanceTracker.UseCases.Transaction.GetById;
using MyFinanceTracker.UseCases.Transaction.Update;

namespace MyFinanceTracker.Integration.Tests;

public sealed class PipelineIntegrationTests
{
    [Fact]
    public async Task ProcessInput_ColdStartAndAddTransaction_SyncsCategoryAndExportsTransactionToSheets()
    {
        // arrange
        await using var fixture = new IntegrationTestFixture();
        fixture.SheetsClient.SeedCategory("food", "Food", isArchived: false);

        await fixture.InitializeDatabase();

        await using (var dbContext = await fixture.CreateDbContext())
        {
            var categories = await dbContext.Categories.ToListAsync();
            categories.Should().ContainSingle(c => c.Id == "food" && c.Name == "Food" && !c.IsArchived);
        }

        fixture.GeminiHandler.EnqueueFunctionCall("add_transaction", new
        {
            type = "expense",
            amounts = new[] { 15.50m },
            categoryId = "food",
            date = "2026-11-10T14:30:00",
            note = "Coffee"
        });

        // act
        var result = await fixture.SendInput(new Input.Text("coffee 15.50"));

        // assert
        var completed = result.Should().BeOfType<ProcessingResult.Completed>().Subject;
        var createdResponse = completed.Actions.Should().ContainSingle()
            .Which.Should().BeOfType<CreateTransactionsResponse.Success>().Subject;
        var createdTransaction = createdResponse.Transactions.Should().ContainSingle().Subject;

        createdTransaction.Type.Should().Be(TransactionType.Expense);
        createdTransaction.Amount.Should().Be(15.50m);
        createdTransaction.Category.Id.Should().Be("food");
        createdTransaction.Category.Name.Should().Be("Food");
        createdTransaction.Date.Should().Be(new DateTime(2026, 11, 10, 14, 30, 0));
        createdTransaction.Note.Should().Be("Coffee");

        await using (var dbContext = await fixture.CreateDbContext())
        {
            var storedTransaction = await dbContext.Transactions.SingleAsync();
            storedTransaction.Id.Should().Be(createdTransaction.Id);
            storedTransaction.Amount.Should().Be(15.50m);
            storedTransaction.CategoryId.Should().Be("food");

            var outboxMessages = await dbContext.OutboxMessages.ToListAsync();
            outboxMessages.Should().ContainSingle(m => m.OperationType == OutboxOperationType.AddTransaction);
        }

        fixture.SheetsClient.GetDataRows(InMemoryGoogleSheetsClient.TransactionsSheetName).Should().BeEmpty();

        // act - flush outbox
        await fixture.OutboxProcessor.ProcessPendingMessages(CancellationToken.None);

        // assert - synced to Google Sheets
        await using (var dbContext = await fixture.CreateDbContext())
        {
            (await dbContext.OutboxMessages.ToListAsync()).Should().BeEmpty();
            (await dbContext.DeadLetterMessages.ToListAsync()).Should().BeEmpty();
        }

        var sheetRows = fixture.SheetsClient.GetSheetRows(InMemoryGoogleSheetsClient.TransactionsSheetName);
        sheetRows.Should().HaveCount(2);
        sheetRows[0].Index.Should().Be(0);
        sheetRows[1].Index.Should().Be(1);
        sheetRows[1].Values.Values.Should().Equal(
            createdTransaction.Id.ToString(),
            "2026-11-10 14:30:00",
            "Expense",
            "food",
            "-15.5",
            "Coffee");
    }

    [Fact]
    public async Task ProcessInput_TransactionFullLifecycle_CreatesReadsUpdatesQueriesAndDeletesAcrossSqliteAndSheets()
    {
        // arrange
        await using var fixture = new IntegrationTestFixture();
        fixture.SheetsClient.SeedCategory("food", "Food", isArchived: false);
        fixture.SheetsClient.SeedCategory("transport", "Transport", isArchived: false);

        await fixture.InitializeDatabase();

        fixture.GeminiHandler.EnqueueFunctionCall("add_transaction", new
        {
            type = "expense",
            amounts = new[] { 25m },
            categoryId = "food",
            date = "2026-11-11T10:00:00",
            note = "Lunch"
        });

        // act & assert - create
        var createResult = await fixture.SendInput(new Input.Text("lunch 25"));
        var createdTx = createResult.Should().BeOfType<ProcessingResult.Completed>().Subject
            .Actions.Should().ContainSingle().Which
            .Should().BeOfType<CreateTransactionsResponse.Success>().Subject
            .Transactions.Should().ContainSingle().Subject;

        await fixture.OutboxProcessor.ProcessPendingMessages(CancellationToken.None);

        var initialSheetRows = fixture.SheetsClient.GetDataRows(InMemoryGoogleSheetsClient.TransactionsSheetName);
        initialSheetRows.Should().ContainSingle();
        var originalRowIndex = initialSheetRows[0].Index;
        originalRowIndex.Should().Be(1);

        // act & assert - get by id
        fixture.GeminiHandler.EnqueueFunctionCall("get_transaction_by_id", new
        {
            id = createdTx.Id.ToString()
        });

        var getResult = await fixture.SendInput(new Input.Text($"show {createdTx.Id}"));
        var fetchedTx = getResult.Should().BeOfType<ProcessingResult.Completed>().Subject
            .Actions.Should().ContainSingle().Which
            .Should().BeOfType<GetTransactionByIdResponse.Success>().Subject
            .Transaction;

        fetchedTx.Id.Should().Be(createdTx.Id);
        fetchedTx.Amount.Should().Be(25m);
        fetchedTx.Category.Id.Should().Be("food");
        fetchedTx.Note.Should().Be("Lunch");

        // act & assert - update
        fixture.GeminiHandler.EnqueueFunctionCall("update_transaction", new
        {
            id = createdTx.Id.ToString(),
            amount = 42.75m,
            categoryId = "transport",
            note = "Taxi home"
        });

        var updateResult = await fixture.SendInput(new Input.Text($"update {createdTx.Id}"));
        var updatedTx = updateResult.Should().BeOfType<ProcessingResult.Completed>().Subject
            .Actions.Should().ContainSingle().Which
            .Should().BeOfType<UpdateTransactionResponse.Success>().Subject
            .Transaction;

        updatedTx.Id.Should().Be(createdTx.Id);
        updatedTx.Amount.Should().Be(42.75m);
        updatedTx.Category.Id.Should().Be("transport");
        updatedTx.Note.Should().Be("Taxi home");

        await fixture.OutboxProcessor.ProcessPendingMessages(CancellationToken.None);

        var updatedSheetRows = fixture.SheetsClient.GetDataRows(InMemoryGoogleSheetsClient.TransactionsSheetName);
        updatedSheetRows.Should().ContainSingle();
        updatedSheetRows[0].Index.Should().Be(originalRowIndex);
        updatedSheetRows[0].Values.Values.Should().Equal(
            createdTx.Id.ToString(),
            "2026-11-11 10:00:00",
            "Expense",
            "transport",
            "-42.75",
            "Taxi home");

        // act & assert - get transactions for date range
        fixture.GeminiHandler.EnqueueFunctionCall("get_transactions", new
        {
            fromDate = "2026-11-11T00:00:00",
            toDate = "2026-11-11T23:59:59"
        });

        var getRangeResult = await fixture.SendInput(new Input.Text("show transactions for 2026-11-11"));
        var getRangeResponse = getRangeResult.Should().BeOfType<ProcessingResult.Completed>().Subject
            .Actions.Should().ContainSingle().Which
            .Should().BeOfType<GetTransactionsResponse.Success>().Subject;

        getRangeResponse.Transactions.TotalCount.Should().Be(1);
        var queriedTx = getRangeResponse.Transactions.Items.Should().ContainSingle().Subject;
        queriedTx.Id.Should().Be(createdTx.Id);
        queriedTx.Amount.Should().Be(42.75m);
        queriedTx.Category.Id.Should().Be("transport");
        queriedTx.Note.Should().Be("Taxi home");

        // act & assert - delete
        fixture.GeminiHandler.EnqueueFunctionCall("delete_transactions", new
        {
            ids = new[] { createdTx.Id.ToString() }
        });

        var deleteResult = await fixture.SendInput(new Input.Text($"delete {createdTx.Id}"));
        var deleteResponse = deleteResult.Should().BeOfType<ProcessingResult.Completed>().Subject
            .Actions.Should().ContainSingle().Which
            .Should().BeOfType<DeleteTransactionsResponse.Success>().Subject;
        deleteResponse.DeletedCount.Should().Be(1);

        await fixture.OutboxProcessor.ProcessPendingMessages(CancellationToken.None);

        await using (var dbContext = await fixture.CreateDbContext())
        {
            (await dbContext.Transactions.ToListAsync()).Should().BeEmpty();
            (await dbContext.OutboxMessages.ToListAsync()).Should().BeEmpty();
        }

        fixture.SheetsClient.GetDataRows(InMemoryGoogleSheetsClient.TransactionsSheetName).Should().BeEmpty();
        fixture.SheetsClient.GetSheetRows(InMemoryGoogleSheetsClient.TransactionsSheetName).Should().ContainSingle(r => r.Index == 0);
    }

    [Fact]
    public async Task ProcessInput_CategoryLifecycleAndArchivedCategory_UpdatesSheetsAndExcludesFromList()
    {
        // arrange
        await using var fixture = new IntegrationTestFixture();
        fixture.SheetsClient.SeedCategory("food", "Food", isArchived: false);
        await fixture.InitializeDatabase();

        fixture.GeminiHandler.EnqueueFunctionCall("create_category", new
        {
            id = "subscriptions",
            name = "Subscriptions"
        });

        // act & assert - create category
        var createCategoryResult = await fixture.SendInput(new Input.Text("create category Subscriptions"));
        var createdCategory = createCategoryResult.Should().BeOfType<ProcessingResult.Completed>().Subject
            .Actions.Should().ContainSingle().Which
            .Should().BeOfType<CreateCategoryResponse.Success>().Subject
            .Category;

        createdCategory.Id.Should().Be("subscriptions");
        createdCategory.Name.Should().Be("Subscriptions");
        createdCategory.IsArchived.Should().BeFalse();

        await fixture.OutboxProcessor.ProcessPendingMessages(CancellationToken.None);

        var createdSheetRows = fixture.SheetsClient.GetDataRows(InMemoryGoogleSheetsClient.CategoriesSheetName);
        createdSheetRows.Should().HaveCount(2);
        createdSheetRows[1].Index.Should().Be(2);
        createdSheetRows[1].Values.Values.Should().Equal("subscriptions", "Subscriptions", "FALSE");

        // act & assert - archive and rename category
        fixture.GeminiHandler.EnqueueFunctionCall("update_category", new
        {
            id = "subscriptions",
            name = "Old Subscriptions",
            isArchived = true
        });

        var updateCategoryResult = await fixture.SendInput(
            new Input.Text("rename subscriptions to Old Subscriptions and archive"));
        var updatedCategory = updateCategoryResult.Should().BeOfType<ProcessingResult.Completed>().Subject
            .Actions.Should().ContainSingle().Which
            .Should().BeOfType<UpdateCategoryResponse.Success>().Subject
            .Category;

        updatedCategory.Id.Should().Be("subscriptions");
        updatedCategory.Name.Should().Be("Old Subscriptions");
        updatedCategory.IsArchived.Should().BeTrue();

        await fixture.OutboxProcessor.ProcessPendingMessages(CancellationToken.None);

        var updatedSheetRows = fixture.SheetsClient.GetDataRows(InMemoryGoogleSheetsClient.CategoriesSheetName);
        updatedSheetRows.Should().HaveCount(2);
        updatedSheetRows[1].Index.Should().Be(2);
        updatedSheetRows[1].Values.Values.Should().Equal("subscriptions", "Old Subscriptions", "TRUE");

        // act & assert - list categories excludes archived category
        fixture.GeminiHandler.EnqueueFunctionCall("list_categories", new { });

        var listResult = await fixture.SendInput(new Input.Text("list categories"));
        var listResponse = listResult.Should().BeOfType<ProcessingResult.Completed>().Subject
            .Actions.Should().ContainSingle().Which
            .Should().BeOfType<ListCategoriesResponse.Success>().Subject;

        listResponse.Categories.Should().ContainSingle(c => c.Id == "food" && !c.IsArchived);
        listResponse.Categories.Should().NotContain(c => c.Id == "subscriptions");
    }

    [Fact]
    public async Task ProcessInput_PdfDocumentWithAddTransaction_SavesTransactionCreatesOutboxAndSyncsToSheets()
    {
        // arrange
        await using var fixture = new IntegrationTestFixture();
        fixture.SheetsClient.SeedCategory("food", "Food", isArchived: false);
        await fixture.InitializeDatabase();

        fixture.GeminiHandler.EnqueueFunctionCall("add_transaction", new
        {
            type = "expense",
            amounts = new[] { 32.45m },
            categoryId = "food",
            date = "2026-11-12T18:15:00",
            note = "Supermarket receipt"
        });

        byte[] pdfBytes = [0x25, 0x50, 0x44, 0x46];
        var documentInput = new Input.Document(pdfBytes, "application/pdf", "November receipt");

        // act - process document input
        var result = await fixture.SendInput(documentInput);

        // assert - saved to SQLite & Outbox
        var completed = result.Should().BeOfType<ProcessingResult.Completed>().Subject;
        var createdResponse = completed.Actions.Should().ContainSingle()
            .Which.Should().BeOfType<CreateTransactionsResponse.Success>().Subject;
        var createdTransaction = createdResponse.Transactions.Should().ContainSingle().Subject;

        createdTransaction.Type.Should().Be(TransactionType.Expense);
        createdTransaction.Amount.Should().Be(32.45m);
        createdTransaction.Category.Id.Should().Be("food");
        createdTransaction.Category.Name.Should().Be("Food");
        createdTransaction.Date.Should().Be(new DateTime(2026, 11, 12, 18, 15, 0));
        createdTransaction.Note.Should().Be("Supermarket receipt");

        await using (var dbContext = await fixture.CreateDbContext())
        {
            var storedTransaction = await dbContext.Transactions.SingleAsync();
            storedTransaction.Id.Should().Be(createdTransaction.Id);
            storedTransaction.Amount.Should().Be(32.45m);
            storedTransaction.CategoryId.Should().Be("food");

            var outboxMessages = await dbContext.OutboxMessages.ToListAsync();
            outboxMessages.Should().ContainSingle(m => m.OperationType == OutboxOperationType.AddTransaction);
        }

        fixture.SheetsClient.GetDataRows(InMemoryGoogleSheetsClient.TransactionsSheetName).Should().BeEmpty();

        // act - flush outbox
        await fixture.OutboxProcessor.ProcessPendingMessages(CancellationToken.None);

        // assert - synced to Google Sheets
        await using (var dbContext = await fixture.CreateDbContext())
        {
            (await dbContext.OutboxMessages.ToListAsync()).Should().BeEmpty();
            (await dbContext.DeadLetterMessages.ToListAsync()).Should().BeEmpty();
        }

        var sheetRows = fixture.SheetsClient.GetDataRows(InMemoryGoogleSheetsClient.TransactionsSheetName);
        var syncedRow = sheetRows.Should().ContainSingle().Subject;
        syncedRow.Index.Should().Be(1);
        syncedRow.Values.Values.Should().Equal(
            createdTransaction.Id.ToString(),
            "2026-11-12 18:15:00",
            "Expense",
            "food",
            "-32.45",
            "Supermarket receipt");
    }
}
