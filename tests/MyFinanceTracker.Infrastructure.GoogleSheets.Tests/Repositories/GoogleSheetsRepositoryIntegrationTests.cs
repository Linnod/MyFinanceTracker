using Google.Apis.Sheets.v4.Data;
using Microsoft.Extensions.Options;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Infrastructure.GoogleSheets.Clients;
using MyFinanceTracker.Infrastructure.GoogleSheets.Configuration;
using MyFinanceTracker.Infrastructure.GoogleSheets.Mapping;
using MyFinanceTracker.Infrastructure.GoogleSheets.Repositories;
using MyFinanceTracker.Infrastructure.GoogleSheets.Services;
using NSubstitute;

namespace MyFinanceTracker.Infrastructure.GoogleSheets.Tests.Repositories;

public class GoogleSheetsRepositoryIntegrationTests
{
    [Fact]
    public async Task AddRange_ShouldOrchestrateMappingAndBatchUpdateCorrectly()
    {
        // arrange
        var options = Options.Create(new GoogleSheetsOptions
        {
            HeaderRowsCount = 2,
            DecimalSeparator = ".",
            SpreadsheetId = "test-spreadsheet-id"
        });

        var mapper = new GoogleSheetMapper(options);
        var formulaBuilder = new FormulaBuilder();
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var formulaService = new FormulaService(clientMock, formulaBuilder);
        var sut = new GoogleSheetsTransactionRepository(mapper, clientMock, formulaService);

        var date = new DateOnly(2026, 01, 24);
        var category = new Category("A", "Food", ["food"], false);
        var transaction = new Transaction(Guid.NewGuid(), TransactionType.Expense, category, 50m, date, "Coffee");

        clientMock.GetFormulas(Arg.Any<IList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<List<string>>(["100"]));

        // act
        await sut.AddRange([transaction], CancellationToken.None);

        // assert
        await clientMock.Received(1).SendBatchUpdate(
            Arg.Is<List<ValueRange>>(list =>
                list.Count == 1 &&
                list[0].Range == "2026.01!A26" &&
                list[0].Values[0][0].ToString() == "=100-50"
            ),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteRange_ShouldSendZeroWithoutFetchingFormulas()
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
        var formulaService = new FormulaService(clientMock, new FormulaBuilder());
        var sut = new GoogleSheetsTransactionRepository(mapper, clientMock, formulaService);

        var date = new DateOnly(2026, 01, 27);
        var category = new Category("B", "Food", ["food"], false);

        // act
        await sut.DeleteRange(category, date, CancellationToken.None);

        // assert
        await clientMock.DidNotReceive().GetFormulas(Arg.Any<IList<string>>(), Arg.Any<CancellationToken>());

        await clientMock.Received(1).SendBatchUpdate(
            Arg.Is<List<ValueRange>>(list =>
                list.Count == 1 &&
                list[0].Range == "2026.01!B29" &&
                list[0].Values[0][0].ToString() == "0"
            ),
            Arg.Any<CancellationToken>());
    }
}