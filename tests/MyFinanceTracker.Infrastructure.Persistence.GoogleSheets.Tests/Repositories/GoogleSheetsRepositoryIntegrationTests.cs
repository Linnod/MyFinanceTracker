using Google.Apis.Sheets.v4.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Clients;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Configuration;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Mapping;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Models;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Services;
using NSubstitute;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Tests.Repositories;

public class GoogleSheetsRepositoryIntegrationTests
{
    [Fact]
    async Task AddRange_ShouldOrchestrateMappingAndBatchUpdateCorrectly()
    {
        // arrange
        var options = Options.Create(new GoogleSheetsOptions
        {
            HeaderRowsCount = 2,
            DecimalSeparator = ".",
            SpreadsheetId = "test-spreadsheet-id"
        });

        var mapper = new GoogleSheetMapper(options);
        var formulaBuilder = new FormulaBuilder(options);
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsTransactionRepository>>();
        var sut = new GoogleSheetsTransactionRepository(
            mapper,
            clientMock,
            formulaBuilder,
            loggerMock);

        var date = new DateOnly(2026, 01, 24);
        var category = new Category("A", "Food", ["food"], false);
        var transaction = new Transaction(
            Guid.NewGuid(),
            TransactionType.Expense,
            category,
            50m,
            date,
            "Coffee");

        clientMock.GetCells(
                Arg.Any<IEnumerable<GoogleSheetCellAddress>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<GoogleSheetCell>>(
            [
                new GoogleSheetCell(new GoogleSheetCellAddress("2026.01", 26, "A"), "100")
            ]));

        // act
        await sut.AddRange([transaction], CancellationToken.None);

        // assert
        var expectedCells = new[]
        {
            new GoogleSheetCell(
                new GoogleSheetCellAddress("2026.01", 26, "A"),
                "=100-50")
        };

        await clientMock.Received(1).SendBatchUpdate(
            Arg.Is<IEnumerable<GoogleSheetCell>>(cells =>
                cells.SequenceEqual(expectedCells)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    async Task DeleteRange_ShouldSendZeroWithoutFetchingCells()
    {
        // arrange
        var options = Options.Create(new GoogleSheetsOptions
        {
            HeaderRowsCount = 2,
            DecimalSeparator = ".",
            SpreadsheetId = "test-id"
        });

        var mapper = new GoogleSheetMapper(options);
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsTransactionRepository>>();
        var formulaBuilder = new FormulaBuilder(options);
        var sut = new GoogleSheetsTransactionRepository(
            mapper,
            clientMock,
            formulaBuilder,
            loggerMock);

        var date = new DateOnly(2026, 01, 27);
        var category = new Category("B", "Food", ["food"], false);

        // act
        await sut.DeleteRange(category, date, CancellationToken.None);

        // assert
        await clientMock.DidNotReceive().GetCells(
            Arg.Any<IEnumerable<GoogleSheetCellAddress>>(),
            Arg.Any<CancellationToken>());

        var expectedCells = new[]
        {
        new GoogleSheetCell(
            new GoogleSheetCellAddress("2026.01", 29, "B"),
            "0")
    };

        await clientMock.Received(1).SendBatchUpdate(
            Arg.Is<IEnumerable<GoogleSheetCell>>(cells =>
                cells.SequenceEqual(expectedCells)),
            Arg.Any<CancellationToken>());
    }
}