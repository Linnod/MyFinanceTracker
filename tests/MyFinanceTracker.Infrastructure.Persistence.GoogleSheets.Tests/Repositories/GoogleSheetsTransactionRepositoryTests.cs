using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Clients;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Configuration;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Exceptions;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Mapping;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Models;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Repositories;
using NSubstitute;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Tests.Repositories;

public class GoogleSheetsTransactionRepositoryTests
{
    private readonly GoogleSheetsOptions defaultOptions = new()
    {
        SpreadsheetId = "test-spreadsheet-id",
        TransactionsSheetName = "Transactions",
        DecimalSeparator = "."
    };

    private readonly Category foodCategory = new("food_id", "Food");
    private readonly Category entertainmentCategory = new("ent_id", "Entertainment");
    private readonly Category transportCategory = new("trans_id", "Transport");

    [Fact]
    public async Task AddRange_ShouldAppendRowsViaClient()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetMapper(options);
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var categoryRepoMock = Substitute.For<ICategoryRepository>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsTransactionRepository>>();

        var sut = new GoogleSheetsTransactionRepository(
            options,
            mapper,
            clientMock,
            categoryRepoMock,
            loggerMock);

        var date = new DateTime(2026, 11, 2, 12, 0, 0);
        var transaction = new Transaction(
            Guid.NewGuid(),
            TransactionType.Expense,
            foodCategory,
            50m,
            date,
            "Lunch");

        // act
        await sut.AddRange([transaction], CancellationToken.None);

        // assert
        await clientMock.Received(1).AppendRows(
            "Transactions",
            Arg.Is<IEnumerable<GoogleSheetRowValues>>(rows =>
                rows.Single().Values[3] == "food_id" &&
                rows.Single().Values[4] == "-50"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddRange_ShouldDoNothing_WhenTransactionsListIsEmpty()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetMapper(options);
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var categoryRepoMock = Substitute.For<ICategoryRepository>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsTransactionRepository>>();

        var sut = new GoogleSheetsTransactionRepository(
            options,
            mapper,
            clientMock,
            categoryRepoMock,
            loggerMock);

        // act
        await sut.AddRange([], CancellationToken.None);

        // assert
        await clientMock.DidNotReceive().AppendRows(
            Arg.Any<string>(),
            Arg.Any<IEnumerable<GoogleSheetRowValues>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Get_ShouldSkipHeaderAndReturnFilteredTransactions()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetMapper(options);
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var categoryRepoMock = Substitute.For<ICategoryRepository>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsTransactionRepository>>();

        categoryRepoMock.GetAll(Arg.Any<CancellationToken>())
            .Returns([foodCategory, entertainmentCategory]);

        var targetDate = new DateTime(2026, 11, 2, 0, 0, 0);

        clientMock.GetRows("Transactions", Arg.Any<CancellationToken>())
            .Returns(
            [
                new(0, new(["Id", "Date", "Type", "Category", "Amount", "Note"])),
                new(1, new([Guid.NewGuid().ToString(), "2026-11-02 00:00:00", "Expense", "food_id", "-25.5", "Lunch"])),
                new(2, new([Guid.NewGuid().ToString(), "2026-11-02 00:00:00", "Expense", "ent_id", "-100", "Cinema"])),
                new(3, new([Guid.NewGuid().ToString(), "2026-11-01 00:00:00", "Expense", "food_id", "-15", "Coffee"]))
            ]);

        var sut = new GoogleSheetsTransactionRepository(
            options,
            mapper,
            clientMock,
            categoryRepoMock,
            loggerMock);

        // act
        var result = await sut.Get(new TransactionFilter(targetDate, targetDate, [foodCategory]), CancellationToken.None);

        // assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Amount.Should().Be(25.5m);
        result.Items[0].Category.Should().Be(foodCategory);
        result.Items[0].Date.Should().Be(targetDate);
        result.Items[0].Note.Should().Be("Lunch");
    }

    [Fact]
    public async Task Get_ShouldFilterByDateRangeMultipleCategoriesAndTransactionType()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetMapper(options);
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var categoryRepoMock = Substitute.For<ICategoryRepository>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsTransactionRepository>>();

        categoryRepoMock.GetAll(Arg.Any<CancellationToken>())
            .Returns([foodCategory, entertainmentCategory, transportCategory]);

        clientMock.GetRows("Transactions", Arg.Any<CancellationToken>())
            .Returns(
            [
                new(0, new(["Id", "Date", "Type", "Category", "Amount", "Note"])),
                new(1, new([Guid.NewGuid().ToString(), "2026-11-02 00:00:00", "Expense", "food_id", "-20", "Lunch"])),
                new(2, new([Guid.NewGuid().ToString(), "2026-11-03 00:00:00", "Expense", "ent_id", "-50", "Cinema"])),
                new(3, new([Guid.NewGuid().ToString(), "2026-11-03 00:00:00", "Income", "food_id", "1000", "Refund"])),
                new(4, new([Guid.NewGuid().ToString(), "2026-11-01 00:00:00", "Expense", "food_id", "-10", "Old Coffee"])),
                new(5, new([Guid.NewGuid().ToString(), "2026-11-05 00:00:00", "Expense", "ent_id", "-30", "Future Movie"])),
                new(6, new([Guid.NewGuid().ToString(), "2026-11-02 00:00:00", "Expense", "trans_id", "-15", "Taxi"]))
            ]);

        var sut = new GoogleSheetsTransactionRepository(
            options,
            mapper,
            clientMock,
            categoryRepoMock,
            loggerMock);

        var filter = new TransactionFilter(
            FromDate: new DateTime(2026, 11, 2, 0, 0, 0),
            ToDate: new DateTime(2026, 11, 4, 23, 59, 59),
            Categories: [foodCategory, entertainmentCategory],
            Type: TransactionType.Expense);

        // act
        var result = await sut.Get(filter, CancellationToken.None);

        // assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().Contain(t => t.Note == "Lunch" && t.Category == foodCategory && t.Amount == 20m);
        result.Items.Should().Contain(t => t.Note == "Cinema" && t.Category == entertainmentCategory && t.Amount == 50m);
    }

    [Fact]
    public async Task Get_ShouldThrowGoogleSheetsPersistenceException_WhenRowFailsToMap()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetMapper(options);
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var categoryRepoMock = Substitute.For<ICategoryRepository>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsTransactionRepository>>();

        categoryRepoMock.GetAll(Arg.Any<CancellationToken>())
            .Returns([foodCategory]);

        var headerRow = new GoogleSheetRow(0, new GoogleSheetRowValues(["Id", "Date", "Type", "Category", "Amount", "Note"]));
        var invalidRow = new GoogleSheetRow(1, new GoogleSheetRowValues([
            "bad-guid", "2026-11-02 00:00:00", "Expense", "food_id", "-25.5"
        ]));

        clientMock.GetRows("Transactions", Arg.Any<CancellationToken>())
            .Returns([headerRow, invalidRow]);

        var sut = new GoogleSheetsTransactionRepository(
            options,
            mapper,
            clientMock,
            categoryRepoMock,
            loggerMock);

        // act
        var act = () => sut.Get(new TransactionFilter(new DateTime(2026, 11, 2, 0, 0, 0), new DateTime(2026, 11, 2, 23, 59, 59), [foodCategory]), CancellationToken.None);

        // assert
        var ex = await act.Should().ThrowAsync<GoogleSheetsPersistenceException>();
        ex.Which.Failures.Should().HaveCount(1);
        ex.Which.Failures[0].RowNumber.Should().Be(2);
    }

    [Fact]
    public async Task DeleteRange_ShouldDeleteMatchingRowsViaClient()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetMapper(options);
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var categoryRepoMock = Substitute.For<ICategoryRepository>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsTransactionRepository>>();

        categoryRepoMock.GetAll(Arg.Any<CancellationToken>())
            .Returns([foodCategory, entertainmentCategory]);

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var otherId = Guid.NewGuid();

        var headerRow = new GoogleSheetRow(0, new GoogleSheetRowValues(["Id", "Date", "Type", "Category", "Amount", "Note"]));
        var matchingRow1 = new GoogleSheetRow(1, new GoogleSheetRowValues([
            id1.ToString(), "2026-11-02 00:00:00", "Expense", "food_id", "-20", "Breakfast"
        ]));
        var matchingRow2 = new GoogleSheetRow(2, new GoogleSheetRowValues([
            id2.ToString(), "2026-11-02 00:00:00", "Expense", "food_id", "-30", "Dinner"
        ]));
        var otherCategoryRow = new GoogleSheetRow(3, new GoogleSheetRowValues([
            otherId.ToString(), "2026-11-02 00:00:00", "Expense", "ent_id", "-50", "Movies"
        ]));

        clientMock.GetRows("Transactions", Arg.Any<CancellationToken>())
            .Returns([headerRow, matchingRow1, matchingRow2, otherCategoryRow]);

        var sut = new GoogleSheetsTransactionRepository(
            options,
            mapper,
            clientMock,
            categoryRepoMock,
            loggerMock);

        // act
        var result = await sut.DeleteRange([id1, id2], CancellationToken.None);

        // assert
        result.Should().Be(2);

        await clientMock.Received(1).DeleteRows(
            "Transactions",
            Arg.Is<IReadOnlyCollection<GoogleSheetRow>>(rows =>
                rows.Count == 2 &&
                rows.Contains(matchingRow1) &&
                rows.Contains(matchingRow2)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteRange_ShouldDeleteOnlyExistingRows_WhenSomeIdsDoNotExist()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetMapper(options);
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var categoryRepoMock = Substitute.For<ICategoryRepository>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsTransactionRepository>>();

        categoryRepoMock.GetAll(Arg.Any<CancellationToken>())
            .Returns([foodCategory]);

        var existingId = Guid.NewGuid();
        var missingId = Guid.NewGuid();

        var headerRow = new GoogleSheetRow(0, new GoogleSheetRowValues(["Id", "Date", "Type", "Category", "Amount", "Note"]));
        var otherDateRow = new GoogleSheetRow(1, new GoogleSheetRowValues([
            existingId.ToString(), "2026-11-01 00:00:00", "Expense", "food_id", "-20"
        ]));

        clientMock.GetRows("Transactions", Arg.Any<CancellationToken>())
            .Returns([headerRow, otherDateRow]);

        var sut = new GoogleSheetsTransactionRepository(
            options,
            mapper,
            clientMock,
            categoryRepoMock,
            loggerMock);

        // act
        var result = await sut.DeleteRange([existingId, missingId], CancellationToken.None);

        // assert
        result.Should().Be(1);

        await clientMock.Received(1).DeleteRows(
            "Transactions",
            Arg.Is<IReadOnlyCollection<GoogleSheetRow>>(rows =>
                rows.Count == 1 &&
                rows.Contains(otherDateRow)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteRange_ShouldDoNothing_WhenIdsIsEmpty()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetMapper(options);
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var categoryRepoMock = Substitute.For<ICategoryRepository>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsTransactionRepository>>();

        var sut = new GoogleSheetsTransactionRepository(
            options,
            mapper,
            clientMock,
            categoryRepoMock,
            loggerMock);

        // act
        var result = await sut.DeleteRange([], CancellationToken.None);

        // assert
        result.Should().Be(0);

        await clientMock.DidNotReceive().GetRows(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await clientMock.DidNotReceive().DeleteRows(
            Arg.Any<string>(),
            Arg.Any<IReadOnlyCollection<GoogleSheetRow>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsTransaction()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetMapper(options);
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var categoryRepoMock = Substitute.For<ICategoryRepository>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsTransactionRepository>>();

        categoryRepoMock.GetAll(Arg.Any<CancellationToken>())
            .Returns([foodCategory]);

        var targetId = Guid.NewGuid();
        var date = new DateTime(2026, 11, 2, 12, 0, 0);

        clientMock.GetRows("Transactions", Arg.Any<CancellationToken>())
            .Returns(
            [
                new(0, new(["Id", "Date", "Type", "Category", "Amount", "Note"])),
                new(1, new([targetId.ToString(), "2026-11-02 12:00:00", "Expense", "food_id", "-50.5", "Lunch"]))
            ]);

        var sut = new GoogleSheetsTransactionRepository(
            options,
            mapper,
            clientMock,
            categoryRepoMock,
            loggerMock);

        // act
        var result = await sut.GetById(targetId, CancellationToken.None);

        // assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(targetId);
        result.Type.Should().Be(TransactionType.Expense);
        result.Category.Should().Be(foodCategory);
        result.Amount.Should().Be(50.5m);
        result.Date.Should().Be(date);
        result.Note.Should().Be("Lunch");
    }

    [Fact]
    public async Task GetById_NonExistentId_ReturnsNull()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetMapper(options);
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var categoryRepoMock = Substitute.For<ICategoryRepository>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsTransactionRepository>>();

        categoryRepoMock.GetAll(Arg.Any<CancellationToken>())
            .Returns([foodCategory]);

        clientMock.GetRows("Transactions", Arg.Any<CancellationToken>())
            .Returns(
            [
                new(0, new(["Id", "Date", "Type", "Category", "Amount", "Note"])),
                new(1, new([Guid.NewGuid().ToString(), "2026-11-02 00:00:00", "Expense", "food_id", "-50", "Lunch"]))
            ]);

        var sut = new GoogleSheetsTransactionRepository(
            options,
            mapper,
            clientMock,
            categoryRepoMock,
            loggerMock);

        // act
        var result = await sut.GetById(Guid.NewGuid(), CancellationToken.None);

        // assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Update_ExistingTransaction_CallsClientUpdateRowWithCorrectIndexAndValues()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetMapper(options);
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var categoryRepoMock = Substitute.For<ICategoryRepository>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsTransactionRepository>>();

        categoryRepoMock.GetAll(Arg.Any<CancellationToken>())
            .Returns([foodCategory, transportCategory]);

        var targetId = Guid.NewGuid();
        var otherId = Guid.NewGuid();

        clientMock.GetRows("Transactions", Arg.Any<CancellationToken>())
            .Returns(
            [
                new(0, new(["Id", "Date", "Type", "Category", "Amount", "Note"])),
                new(1, new([otherId.ToString(), "2026-11-01 00:00:00", "Expense", "food_id", "-10", "Coffee"])),
                new(2, new([targetId.ToString(), "2026-11-02 00:00:00", "Expense", "food_id", "-50", "Lunch"]))
            ]);

        var sut = new GoogleSheetsTransactionRepository(
            options,
            mapper,
            clientMock,
            categoryRepoMock,
            loggerMock);

        var updatedTx = new Transaction(
            targetId,
            TransactionType.Income,
            transportCategory,
            350.50m,
            new DateTime(2026, 11, 3, 10, 0, 0),
            "Bonus reimbursement");

        // act
        await sut.Update(updatedTx, CancellationToken.None);

        // assert
        await clientMock.Received(1).UpdateRow(
            "Transactions",
            Arg.Is<GoogleSheetRow>(row =>
                row.Index == 2 &&
                row.Values.Values[0] == targetId.ToString() &&
                row.Values.Values[1] == "2026-11-03 10:00:00" &&
                row.Values.Values[2] == "Income" &&
                row.Values.Values[3] == "trans_id" &&
                row.Values.Values[4] == "350.5" &&
                row.Values.Values[5] == "Bonus reimbursement"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_NonExistentTransaction_ThrowsGoogleSheetsEntityNotFoundException()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetMapper(options);
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var categoryRepoMock = Substitute.For<ICategoryRepository>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsTransactionRepository>>();

        categoryRepoMock.GetAll(Arg.Any<CancellationToken>())
            .Returns([foodCategory]);

        var existingId = Guid.NewGuid();
        var nonExistentId = Guid.NewGuid();

        clientMock.GetRows("Transactions", Arg.Any<CancellationToken>())
            .Returns(
            [
                new(0, new(["Id", "Date", "Type", "Category", "Amount", "Note"])),
                new(1, new([existingId.ToString(), "2026-11-01 00:00:00", "Expense", "food_id", "-10", "Coffee"]))
            ]);

        var sut = new GoogleSheetsTransactionRepository(
            options,
            mapper,
            clientMock,
            categoryRepoMock,
            loggerMock);

        var nonExistentTx = new Transaction(
            nonExistentId,
            TransactionType.Expense,
            foodCategory,
            25m,
            new DateTime(2026, 11, 2, 10, 0, 0),
            null);

        // act
        var act = () => sut.Update(nonExistentTx, CancellationToken.None);

        // assert
        await act.Should().ThrowAsync<GoogleSheetsEntityNotFoundException>()
            .WithMessage($"*{nonExistentId}*");

        await clientMock.DidNotReceiveWithAnyArgs().UpdateRow(default!, default!, default);
    }

    [Fact]
    public async Task GetAll_ShouldReturnAllTransactionsFromSheet()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetMapper(options);
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var categoryRepoMock = Substitute.For<ICategoryRepository>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsTransactionRepository>>();

        categoryRepoMock.GetAll(Arg.Any<CancellationToken>())
            .Returns([foodCategory]);

        var txId1 = Guid.NewGuid();
        var txId2 = Guid.NewGuid();

        clientMock.GetRows("Transactions", Arg.Any<CancellationToken>())
            .Returns(
            [
                new(0, new(["Id", "Date", "Type", "Category", "Amount", "Note"])),
                new(1, new([txId1.ToString(), "2026-11-01 00:00:00", "Expense", "food_id", "-10", "Coffee"])),
                new(2, new([txId2.ToString(), "2026-11-02 00:00:00", "Expense", "food_id", "-20", "Lunch"]))
            ]);

        var sut = new GoogleSheetsTransactionRepository(
            options,
            mapper,
            clientMock,
            categoryRepoMock,
            loggerMock);

        // act
        var results = await sut.GetAll(CancellationToken.None);

        // assert
        results.Should().HaveCount(2);
        results.Select(t => t.Id).Should().BeEquivalentTo([txId1, txId2]);
    }

    [Fact]
    public async Task Get_WithSkipAndTake_ReturnsPagedResultAndTotalCount()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetMapper(options);
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var categoryRepoMock = Substitute.For<ICategoryRepository>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsTransactionRepository>>();

        categoryRepoMock.GetAll(Arg.Any<CancellationToken>())
            .Returns([foodCategory]);

        var txId1 = Guid.NewGuid();
        var txId2 = Guid.NewGuid();
        var txId3 = Guid.NewGuid();

        clientMock.GetRows("Transactions", Arg.Any<CancellationToken>())
            .Returns(
            [
                new(0, new(["Id", "Date", "Type", "Category", "Amount", "Note"])),
                new(1, new([txId1.ToString(), "2026-11-01 00:00:00", "Expense", "food_id", "-10", "Coffee"])),
                new(2, new([txId2.ToString(), "2026-11-02 00:00:00", "Expense", "food_id", "-20", "Lunch"])),
                new(3, new([txId3.ToString(), "2026-11-03 00:00:00", "Expense", "food_id", "-30", "Dinner"]))
            ]);

        var sut = new GoogleSheetsTransactionRepository(
            options,
            mapper,
            clientMock,
            categoryRepoMock,
            loggerMock);

        var filter = new TransactionFilter(Skip: 1, Take: 1);

        // act
        var result = await sut.Get(filter, CancellationToken.None);

        // assert
        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(1);
        result.Items[0].Id.Should().Be(txId2);
    }

    [Fact]
    public async Task GetAll_DuplicateTransactionsInSheet_ThrowsGoogleSheetsPersistenceException()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetMapper(options);
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var categoryRepoMock = Substitute.For<ICategoryRepository>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsTransactionRepository>>();

        categoryRepoMock.GetAll(Arg.Any<CancellationToken>())
            .Returns([foodCategory]);

        var duplicateId = Guid.NewGuid();
        var uniqueId = Guid.NewGuid();

        clientMock.GetRows("Transactions", Arg.Any<CancellationToken>())
            .Returns(
            [
                new(0, new(["Id", "Date", "Type", "Category", "Amount", "Note"])),
                new(1, new([duplicateId.ToString(), "2026-11-01 00:00:00", "Expense", "food_id", "-10", "Coffee"])),
                new(2, new([duplicateId.ToString(), "2026-11-02 00:00:00", "Expense", "food_id", "-20", "Lunch"])),
                new(3, new([uniqueId.ToString(), "2026-11-03 00:00:00", "Expense", "food_id", "-30", "Dinner"]))
            ]);

        using var sut = new GoogleSheetsTransactionRepository(
            options,
            mapper,
            clientMock,
            categoryRepoMock,
            loggerMock);

        // act
        var act = () => sut.GetAll(CancellationToken.None);

        // assert
        var ex = await act.Should().ThrowAsync<GoogleSheetsPersistenceException>();
        ex.Which.Message.Should().Contain(duplicateId.ToString());
        ex.Which.Message.Should().NotContain(uniqueId.ToString());
    }
}
