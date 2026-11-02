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

public class GoogleSheetsRepositoryIntegrationTests
{
    private readonly GoogleSheetsOptions defaultOptions = new()
    {
        SpreadsheetId = "test-spreadsheet-id",
        TransactionsSheetName = "Transactions",
        DecimalSeparator = "."
    };

    private readonly Category foodCategory = new("food_id", "Food", ["food"]);
    private readonly Category entertainmentCategory = new("ent_id", "Entertainment", ["entertainment"]);

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

        var date = new DateOnly(2026, 11, 2);
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
                rows.Single().Values[3] == "Food" &&
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

        var targetDate = new DateOnly(2026, 11, 2);
        var otherDate = new DateOnly(2026, 11, 1);

        var headerRow = new GoogleSheetRow(0, new GoogleSheetRowValues(["Id", "Date", "Type", "Category", "Amount", "Note"]));
        var matchingRow = new GoogleSheetRow(1, new GoogleSheetRowValues([
            Guid.NewGuid().ToString(), "2026-11-02", "Expense", "Food", "-25.5", "Lunch"
        ]));
        var wrongCategoryRow = new GoogleSheetRow(2, new GoogleSheetRowValues([
            Guid.NewGuid().ToString(), "2026-11-02", "Expense", "Entertainment", "-100", "Cinema"
        ]));
        var wrongDateRow = new GoogleSheetRow(3, new GoogleSheetRowValues([
            Guid.NewGuid().ToString(), "2026-11-01", "Expense", "Food", "-15", "Coffee"
        ]));

        clientMock.GetRows("Transactions", Arg.Any<CancellationToken>())
            .Returns([headerRow, matchingRow, wrongCategoryRow, wrongDateRow]);

        var sut = new GoogleSheetsTransactionRepository(
            options,
            mapper,
            clientMock,
            categoryRepoMock,
            loggerMock);

        // act
        var result = await sut.Get(foodCategory, targetDate, CancellationToken.None);

        // assert
        result.Should().HaveCount(1);
        result[0].Amount.Should().Be(25.5m);
        result[0].Category.Should().Be(foodCategory);
        result[0].Date.Should().Be(targetDate);
        result[0].Note.Should().Be("Lunch");
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
            "bad-guid", "2026-11-02", "Expense", "Food", "-25.5"
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
        var act = () => sut.Get(foodCategory, new DateOnly(2026, 11, 2), CancellationToken.None);

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

        var targetDate = new DateOnly(2026, 11, 2);

        var headerRow = new GoogleSheetRow(0, new GoogleSheetRowValues(["Id", "Date", "Type", "Category", "Amount", "Note"]));
        var matchingRow1 = new GoogleSheetRow(1, new GoogleSheetRowValues([
            Guid.NewGuid().ToString(), "2026-11-02", "Expense", "Food", "-20", "Breakfast"
        ]));
        var matchingRow2 = new GoogleSheetRow(2, new GoogleSheetRowValues([
            Guid.NewGuid().ToString(), "2026-11-02", "Expense", "Food", "-30", "Dinner"
        ]));
        var otherCategoryRow = new GoogleSheetRow(3, new GoogleSheetRowValues([
            Guid.NewGuid().ToString(), "2026-11-02", "Expense", "Entertainment", "-50", "Movies"
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
        await sut.DeleteRange(foodCategory, targetDate, CancellationToken.None);

        // assert
        await clientMock.Received(1).DeleteRows(
            "Transactions",
            Arg.Is<IReadOnlyCollection<GoogleSheetRow>>(rows =>
                rows.Count == 2 &&
                rows.Contains(matchingRow1) &&
                rows.Contains(matchingRow2)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteRange_ShouldNotCallDeleteRows_WhenNoRowsMatch()
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
        var otherDateRow = new GoogleSheetRow(1, new GoogleSheetRowValues([
            Guid.NewGuid().ToString(), "2026-11-01", "Expense", "Food", "-20"
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
        await sut.DeleteRange(foodCategory, new DateOnly(2026, 11, 2), CancellationToken.None);

        // assert
        await clientMock.DidNotReceive().DeleteRows(
            Arg.Any<string>(),
            Arg.Any<IReadOnlyCollection<GoogleSheetRow>>(),
            Arg.Any<CancellationToken>());
    }
}
