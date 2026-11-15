using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Clients;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Configuration;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Exceptions;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Mapping;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Models;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Repositories;
using MyFinanceTracker.Infrastructure.Persistence.Models;
using NSubstitute;

namespace MyFinanceTracker.Infrastructure.Persistence.Tests.GoogleSheets.Repositories;

public class GoogleSheetsTransactionRepositoryTests
{
    private readonly GoogleSheetsOptions defaultOptions = new()
    {
        SpreadsheetId = "test-spreadsheet-id",
        TransactionsSheetName = "Transactions"
    };

    [Fact]
    public async Task AddRange_ShouldAppendRowsViaClient()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetTransactionMapper();
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsTransactionRepository>>();

        var sut = new GoogleSheetsTransactionRepository(
            options,
            mapper,
            clientMock,
            loggerMock);

        var date = new DateTime(2026, 11, 2, 12, 0, 0);
        var transaction = new TransactionSyncRecord(
            Guid.NewGuid(),
            TransactionType.Expense,
            "food_id",
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
        var mapper = new GoogleSheetTransactionMapper();
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsTransactionRepository>>();

        var sut = new GoogleSheetsTransactionRepository(
            options,
            mapper,
            clientMock,
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
    public async Task GetAll_WhenRowFailsToMap_ThrowsGoogleSheetsPersistenceException()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetTransactionMapper();
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsTransactionRepository>>();

        var headerRow = new GoogleSheetRow(0, new GoogleSheetRowValues(["Id", "Date", "Type", "Category", "Amount", "Note"]));
        var invalidRow = new GoogleSheetRow(1, new GoogleSheetRowValues([
            "bad-guid", "2026-11-02 00:00:00", "Expense", "food_id", "-25.5"
        ]));

        clientMock.GetRows("Transactions", Arg.Any<CancellationToken>())
            .Returns([headerRow, invalidRow]);

        using var sut = new GoogleSheetsTransactionRepository(
            options,
            mapper,
            clientMock,
            loggerMock);

        // act
        var act = () => sut.GetAll(CancellationToken.None);

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
        var mapper = new GoogleSheetTransactionMapper();
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsTransactionRepository>>();

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
        var mapper = new GoogleSheetTransactionMapper();
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsTransactionRepository>>();

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
        var mapper = new GoogleSheetTransactionMapper();
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsTransactionRepository>>();

        var sut = new GoogleSheetsTransactionRepository(
            options,
            mapper,
            clientMock,
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
    public async Task Update_ExistingTransaction_CallsClientUpdateRowWithCorrectIndexAndValues()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetTransactionMapper();
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsTransactionRepository>>();

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
            loggerMock);

        var updatedTx = new TransactionSyncRecord(
            targetId,
            TransactionType.Income,
            "trans_id",
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
        var mapper = new GoogleSheetTransactionMapper();
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsTransactionRepository>>();

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
            loggerMock);

        var nonExistentTx = new TransactionSyncRecord(
            nonExistentId,
            TransactionType.Expense,
            "food_id",
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
        var mapper = new GoogleSheetTransactionMapper();
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsTransactionRepository>>();

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
            loggerMock);

        // act
        var results = await sut.GetAll(CancellationToken.None);

        // assert
        results.Should().HaveCount(2);
        results.Select(t => t.Id).Should().BeEquivalentTo([txId1, txId2]);
    }

    [Fact]
    public async Task GetAll_DuplicateTransactionsInSheet_ThrowsGoogleSheetsPersistenceException()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetTransactionMapper();
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsTransactionRepository>>();

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
            loggerMock);

        // act
        var act = () => sut.GetAll(CancellationToken.None);

        // assert
        var ex = await act.Should().ThrowAsync<GoogleSheetsPersistenceException>();
        ex.Which.Message.Should().Contain(duplicateId.ToString());
        ex.Which.Message.Should().NotContain(uniqueId.ToString());
    }

    [Fact]
    public async Task Update_DuplicateTransactionIdsInSheet_ThrowsGoogleSheetsPersistenceException()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetTransactionMapper();
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsTransactionRepository>>();

        var duplicateId = Guid.NewGuid();

        clientMock.GetRows("Transactions", Arg.Any<CancellationToken>())
            .Returns(
            [
                new(0, new(["Id", "Date", "Type", "Category", "Amount", "Note"])),
                new(1, new([duplicateId.ToString(), "2026-11-01 00:00:00", "Expense", "food_id", "-10", "Coffee"])),
                new(2, new([duplicateId.ToString(), "2026-11-02 00:00:00", "Expense", "food_id", "-20", "Lunch"]))
            ]);

        using var sut = new GoogleSheetsTransactionRepository(
            options,
            mapper,
            clientMock,
            loggerMock);

        var tx = new TransactionSyncRecord(
            duplicateId,
            TransactionType.Expense,
            "food_id",
            50m,
            new DateTime(2026, 11, 3, 10, 0, 0),
            null);

        // act
        var act = () => sut.Update(tx, CancellationToken.None);

        // assert
        await act.Should().ThrowAsync<GoogleSheetsPersistenceException>()
            .WithMessage($"*{duplicateId}*");
    }

    [Fact]
    public async Task DeleteRange_InvalidIdRowInSheet_ThrowsGoogleSheetsPersistenceException()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetTransactionMapper();
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsTransactionRepository>>();

        clientMock.GetRows("Transactions", Arg.Any<CancellationToken>())
            .Returns(
            [
                new(0, new(["Id", "Date", "Type", "Category", "Amount", "Note"])),
                new(1, new(["bad-guid", "2026-11-01 00:00:00", "Expense", "food_id", "-10"]))
            ]);

        using var sut = new GoogleSheetsTransactionRepository(
            options,
            mapper,
            clientMock,
            loggerMock);

        // act
        var act = () => sut.DeleteRange([Guid.NewGuid()], CancellationToken.None);

        // assert
        var ex = await act.Should().ThrowAsync<GoogleSheetsPersistenceException>();
        ex.Which.Failures.Should().HaveCount(1);
        ex.Which.Failures[0].RowNumber.Should().Be(2);
    }
}
