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
    private readonly Category transportCategory = new("trans_id", "Transport", ["transport"]);

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

        clientMock.GetRows("Transactions", Arg.Any<CancellationToken>())
            .Returns(
            [
                new(0, new(["Id", "Date", "Type", "Category", "Amount", "Note"])),
                new(1, new([Guid.NewGuid().ToString(), "2026-11-02", "Expense", "Food", "-25.5", "Lunch"])),
                new(2, new([Guid.NewGuid().ToString(), "2026-11-02", "Expense", "Entertainment", "-100", "Cinema"])),
                new(3, new([Guid.NewGuid().ToString(), "2026-11-01", "Expense", "Food", "-15", "Coffee"]))
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
        result.Should().HaveCount(1);
        result[0].Amount.Should().Be(25.5m);
        result[0].Category.Should().Be(foodCategory);
        result[0].Date.Should().Be(targetDate);
        result[0].Note.Should().Be("Lunch");
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
                new(1, new([Guid.NewGuid().ToString(), "2026-11-02", "Expense", "Food", "-20", "Lunch"])),
                new(2, new([Guid.NewGuid().ToString(), "2026-11-03", "Expense", "Entertainment", "-50", "Cinema"])),
                new(3, new([Guid.NewGuid().ToString(), "2026-11-03", "Income", "Food", "1000", "Refund"])),
                new(4, new([Guid.NewGuid().ToString(), "2026-11-01", "Expense", "Food", "-10", "Old Coffee"])),
                new(5, new([Guid.NewGuid().ToString(), "2026-11-05", "Expense", "Entertainment", "-30", "Future Movie"])),
                new(6, new([Guid.NewGuid().ToString(), "2026-11-02", "Expense", "Transport", "-15", "Taxi"]))
            ]);

        var sut = new GoogleSheetsTransactionRepository(
            options,
            mapper,
            clientMock,
            categoryRepoMock,
            loggerMock);

        var filter = new TransactionFilter(
            FromDate: new DateOnly(2026, 11, 2),
            ToDate: new DateOnly(2026, 11, 4),
            Categories: [foodCategory, entertainmentCategory],
            Type: TransactionType.Expense);

        // act
        var result = await sut.Get(filter, CancellationToken.None);

        // assert
        result.Should().HaveCount(2);
        result.Should().Contain(t => t.Note == "Lunch" && t.Category == foodCategory && t.Amount == 20m);
        result.Should().Contain(t => t.Note == "Cinema" && t.Category == entertainmentCategory && t.Amount == 50m);
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
        var act = () => sut.Get(new TransactionFilter(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 2), [foodCategory]), CancellationToken.None);

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
            id1.ToString(), "2026-11-02", "Expense", "Food", "-20", "Breakfast"
        ]));
        var matchingRow2 = new GoogleSheetRow(2, new GoogleSheetRowValues([
            id2.ToString(), "2026-11-02", "Expense", "Food", "-30", "Dinner"
        ]));
        var otherCategoryRow = new GoogleSheetRow(3, new GoogleSheetRowValues([
            otherId.ToString(), "2026-11-02", "Expense", "Entertainment", "-50", "Movies"
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
        result.Should().BeOfType<DeleteTransactionsResult.Success>()
            .Which.DeletedCount.Should().Be(2);

        await clientMock.Received(1).DeleteRows(
            "Transactions",
            Arg.Is<IReadOnlyCollection<GoogleSheetRow>>(rows =>
                rows.Count == 2 &&
                rows.Contains(matchingRow1) &&
                rows.Contains(matchingRow2)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteRange_ShouldNotCallDeleteRows_WhenAnyIdDoesNotMatch()
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
            existingId.ToString(), "2026-11-01", "Expense", "Food", "-20"
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
        var notFound = result.Should().BeOfType<DeleteTransactionsResult.NotFound>().Subject;
        notFound.MissingIds.Should().ContainSingle().Which.Should().Be(missingId);

        await clientMock.DidNotReceive().DeleteRows(
            Arg.Any<string>(),
            Arg.Any<IReadOnlyCollection<GoogleSheetRow>>(),
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
        result.Should().BeOfType<DeleteTransactionsResult.Success>()
            .Which.DeletedCount.Should().Be(0);

        await clientMock.DidNotReceive().GetRows(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await clientMock.DidNotReceive().DeleteRows(
            Arg.Any<string>(),
            Arg.Any<IReadOnlyCollection<GoogleSheetRow>>(),
            Arg.Any<CancellationToken>());
    }
}
