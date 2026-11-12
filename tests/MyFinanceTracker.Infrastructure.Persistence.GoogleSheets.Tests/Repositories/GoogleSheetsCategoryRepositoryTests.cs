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
using NSubstitute;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Tests.Repositories;

public class GoogleSheetsCategoryRepositoryTests
{
    private readonly GoogleSheetsOptions defaultOptions = new()
    {
        SpreadsheetId = "test-spreadsheet-id",
        CategoriesSheetName = "Categories"
    };

    private readonly Category foodCategory = new("food_id", "Food", isArchived: false);

    [Fact]
    public async Task GetAll_ValidRows_SkipsHeaderAndReturnsCategories()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetCategoryMapper();
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsCategoryRepository>>();

        var headerRow = new GoogleSheetRow(0, new GoogleSheetRowValues(["Id", "Name", "IsArchived"]));
        var row1 = new GoogleSheetRow(1, new GoogleSheetRowValues(["food_id", "Food", "FALSE"]));
        var row2 = new GoogleSheetRow(2, new GoogleSheetRowValues(["rent_id", "Rent", "TRUE"]));

        clientMock.GetRows("Categories", Arg.Any<CancellationToken>())
            .Returns([headerRow, row1, row2]);

        using var sut = new GoogleSheetsCategoryRepository(options, mapper, clientMock, loggerMock);

        // act
        var results = await sut.GetAll(CancellationToken.None);

        // assert
        results.Should().HaveCount(2);
        results[0].Id.Should().Be("food_id");
        results[0].Name.Should().Be("Food");
        results[0].IsArchived.Should().BeFalse();
        results[1].Id.Should().Be("rent_id");
        results[1].Name.Should().Be("Rent");
        results[1].IsArchived.Should().BeTrue();
    }

    [Fact]
    public async Task GetAll_HeaderOnly_ReturnsEmptyList()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetCategoryMapper();
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsCategoryRepository>>();

        var headerRow = new GoogleSheetRow(0, new GoogleSheetRowValues(["Id", "Name", "IsArchived"]));

        clientMock.GetRows("Categories", Arg.Any<CancellationToken>())
            .Returns([headerRow]);

        using var sut = new GoogleSheetsCategoryRepository(options, mapper, clientMock, loggerMock);

        // act
        var results = await sut.GetAll(CancellationToken.None);

        // assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_WhenSheetIsEmpty_ReturnsEmptyList()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetCategoryMapper();
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsCategoryRepository>>();

        clientMock.GetRows("Categories", Arg.Any<CancellationToken>())
            .Returns([]);

        using var sut = new GoogleSheetsCategoryRepository(options, mapper, clientMock, loggerMock);

        // act
        var results = await sut.GetAll(CancellationToken.None);

        // assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_WhenMappingFailuresExist_ThrowsGoogleSheetsPersistenceException()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetCategoryMapper();
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsCategoryRepository>>();

        var headerRow = new GoogleSheetRow(0, new GoogleSheetRowValues(["Id", "Name", "IsArchived"]));
        var invalidRow = new GoogleSheetRow(1, new GoogleSheetRowValues(["", "Food", "FALSE"]));

        clientMock.GetRows("Categories", Arg.Any<CancellationToken>())
            .Returns([headerRow, invalidRow]);

        using var sut = new GoogleSheetsCategoryRepository(options, mapper, clientMock, loggerMock);

        // act
        var act = () => sut.GetAll(CancellationToken.None);

        // assert
        var ex = await act.Should().ThrowAsync<GoogleSheetsPersistenceException>();
        ex.Which.Failures.Should().HaveCount(1);
        ex.Which.Failures[0].RowNumber.Should().Be(2);
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsCategoryCaseInsensitively()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetCategoryMapper();
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsCategoryRepository>>();

        var headerRow = new GoogleSheetRow(0, new GoogleSheetRowValues(["Id", "Name", "IsArchived"]));
        var row1 = new GoogleSheetRow(1, new GoogleSheetRowValues(["food_id", "Food", "FALSE"]));

        clientMock.GetRows("Categories", Arg.Any<CancellationToken>())
            .Returns([headerRow, row1]);

        using var sut = new GoogleSheetsCategoryRepository(options, mapper, clientMock, loggerMock);

        // act
        var result = await sut.GetById("FOOD_ID", CancellationToken.None);

        // assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("food_id");
        result.Name.Should().Be("Food");
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNull()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetCategoryMapper();
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsCategoryRepository>>();

        var headerRow = new GoogleSheetRow(0, new GoogleSheetRowValues(["Id", "Name", "IsArchived"]));
        var row1 = new GoogleSheetRow(1, new GoogleSheetRowValues(["food_id", "Food", "FALSE"]));

        clientMock.GetRows("Categories", Arg.Any<CancellationToken>())
            .Returns([headerRow, row1]);

        using var sut = new GoogleSheetsCategoryRepository(options, mapper, clientMock, loggerMock);

        // act
        var result = await sut.GetById("non_existing", CancellationToken.None);

        // assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetById_NullOrWhitespaceId_ReturnsNullWithoutCallingClient(string? id)
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetCategoryMapper();
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsCategoryRepository>>();

        using var sut = new GoogleSheetsCategoryRepository(options, mapper, clientMock, loggerMock);

        // act
        var result = await sut.GetById(id!, CancellationToken.None);

        // assert
        result.Should().BeNull();
        await clientMock.DidNotReceive().GetRows(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIds_ExistingIds_ReturnsMatchingCategoriesIgnoringCaseAndDuplicates()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetCategoryMapper();
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsCategoryRepository>>();

        var headerRow = new GoogleSheetRow(0, new GoogleSheetRowValues(["Id", "Name", "IsArchived"]));
        var row1 = new GoogleSheetRow(1, new GoogleSheetRowValues(["food_id", "Food", "FALSE"]));
        var row2 = new GoogleSheetRow(2, new GoogleSheetRowValues(["rent_id", "Rent", "TRUE"]));
        var row3 = new GoogleSheetRow(3, new GoogleSheetRowValues(["other_id", "Other", "FALSE"]));

        clientMock.GetRows("Categories", Arg.Any<CancellationToken>())
            .Returns([headerRow, row1, row2, row3]);

        using var sut = new GoogleSheetsCategoryRepository(options, mapper, clientMock, loggerMock);

        // act
        var results = await sut.GetByIds(["FOOD_ID", "rent_id", "food_id", "missing_id"], CancellationToken.None);

        // assert
        results.Should().HaveCount(2);
        results.Select(c => c.Id).Should().Equal("food_id", "rent_id");
    }

    [Fact]
    public async Task GetByIds_EmptyCollection_ReturnsEmptyListWithoutCallingClient()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetCategoryMapper();
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsCategoryRepository>>();

        using var sut = new GoogleSheetsCategoryRepository(options, mapper, clientMock, loggerMock);

        // act
        var results = await sut.GetByIds([], CancellationToken.None);

        // assert
        results.Should().BeEmpty();
        await clientMock.DidNotReceive().GetRows(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIds_WhitespaceOnlyIds_ReturnsEmptyListWithoutCallingClient()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetCategoryMapper();
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsCategoryRepository>>();

        using var sut = new GoogleSheetsCategoryRepository(options, mapper, clientMock, loggerMock);

        // act
        var results = await sut.GetByIds(["  ", ""], CancellationToken.None);

        // assert
        results.Should().BeEmpty();
        await clientMock.DidNotReceive().GetRows(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Add_ValidCategory_AppendsRowViaClient()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetCategoryMapper();
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsCategoryRepository>>();

        using var sut = new GoogleSheetsCategoryRepository(options, mapper, clientMock, loggerMock);

        // act
        await sut.Add(foodCategory, CancellationToken.None);

        // assert
        await clientMock.Received(1).AppendRows(
            "Categories",
            Arg.Is<IEnumerable<GoogleSheetRowValues>>(rows =>
                rows.Single().Values.SequenceEqual(new[] { "food_id", "Food", "FALSE" })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_ExistingCategory_UpdatesRowViaClient()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetCategoryMapper();
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsCategoryRepository>>();

        var headerRow = new GoogleSheetRow(0, new GoogleSheetRowValues(["Id", "Name", "IsArchived"]));
        var row1 = new GoogleSheetRow(1, new GoogleSheetRowValues(["food_id", "Old Food", "FALSE"]));
        var row2 = new GoogleSheetRow(2, new GoogleSheetRowValues(["rent_id", "Rent", "FALSE"]));

        clientMock.GetRows("Categories", Arg.Any<CancellationToken>())
            .Returns([headerRow, row1, row2]);

        using var sut = new GoogleSheetsCategoryRepository(options, mapper, clientMock, loggerMock);
        var updatedCategory = new Category("FOOD_ID", "Updated Food", isArchived: true);

        // act
        await sut.Update(updatedCategory, CancellationToken.None);

        // assert
        await clientMock.Received(1).UpdateRow(
            "Categories",
            Arg.Is<GoogleSheetRow>(r =>
                r.Index == 1 &&
                r.Values.Values.SequenceEqual(new[] { "FOOD_ID", "Updated Food", "TRUE" })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_NonExistingCategory_ThrowsGoogleSheetsPersistenceException()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetCategoryMapper();
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsCategoryRepository>>();

        var headerRow = new GoogleSheetRow(0, new GoogleSheetRowValues(["Id", "Name", "IsArchived"]));
        var row1 = new GoogleSheetRow(1, new GoogleSheetRowValues(["food_id", "Food", "FALSE"]));

        clientMock.GetRows("Categories", Arg.Any<CancellationToken>())
            .Returns([headerRow, row1]);

        using var sut = new GoogleSheetsCategoryRepository(options, mapper, clientMock, loggerMock);
        var unknownCategory = new Category("unknown_id", "Unknown", isArchived: false);

        // act
        var act = () => sut.Update(unknownCategory, CancellationToken.None);

        // assert
        var ex = await act.Should().ThrowAsync<GoogleSheetsPersistenceException>();
        ex.WithMessage("Category 'unknown_id' was not found in Google Sheet 'Categories'.");
    }

    [Fact]
    public void Dispose_DisposesSemaphore()
    {
        // arrange
        var options = Options.Create(defaultOptions);
        var mapper = new GoogleSheetCategoryMapper();
        var clientMock = Substitute.For<IGoogleSheetsClient>();
        var loggerMock = Substitute.For<ILogger<GoogleSheetsCategoryRepository>>();

        var sut = new GoogleSheetsCategoryRepository(options, mapper, clientMock, loggerMock);

        // act
        var act = () => sut.Dispose();

        // assert
        act.Should().NotThrow();
    }
}
