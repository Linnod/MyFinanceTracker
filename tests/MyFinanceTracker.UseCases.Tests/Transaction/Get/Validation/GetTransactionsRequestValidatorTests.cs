using FluentAssertions;
using FluentValidation.TestHelper;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.UseCases.Transaction.Get;
using MyFinanceTracker.UseCases.Transaction.Get.Validation;

namespace MyFinanceTracker.UseCases.Tests.Transaction.Get.Validation;

public class GetTransactionsRequestValidatorTests
{
    private readonly GetTransactionsRequestValidator sut;

    public GetTransactionsRequestValidatorTests()
    {
        sut = new GetTransactionsRequestValidator();
    }

    [Fact]
    public void Validate_ValidRequestWithCategories_ReturnsNoErrors()
    {
        // arrange
        var request = new GetTransactionsRequest(
            FromDate: new DateTime(2026, 1, 1),
            ToDate: new DateTime(2026, 1, 27),
            CategoryIds: ["food", "taxi"],
            Type: TransactionType.Expense);

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ValidRequestWithoutCategories_ReturnsNoErrors()
    {
        // arrange
        var request = new GetTransactionsRequest(
            FromDate: new DateTime(2026, 1, 1),
            ToDate: new DateTime(2026, 1, 27),
            CategoryIds: null,
            Type: null);

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void Validate_FromDateOutOfRange_ReturnsDateOutOfRange(int offset)
    {
        // arrange
        var year = offset < 0
            ? FinancialRules.MinAllowedYear - 1
            : FinancialRules.MaxAllowedYear + 1;

        var request = new GetTransactionsRequest(
            FromDate: new DateTime(year, 1, 1),
            ToDate: new DateTime(2026, 1, 27));

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldHaveValidationErrorFor(x => x.FromDate.Year)
            .WithErrorCode(ValidationErrorCode.Transaction.DateOutOfRange);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void Validate_ToDateOutOfRange_ReturnsDateOutOfRange(int offset)
    {
        // arrange
        var year = offset < 0
            ? FinancialRules.MinAllowedYear - 1
            : FinancialRules.MaxAllowedYear + 1;

        var request = new GetTransactionsRequest(
            FromDate: new DateTime(2026, 1, 1),
            ToDate: new DateTime(year, 1, 1));

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldHaveValidationErrorFor(x => x.ToDate.Year)
            .WithErrorCode(ValidationErrorCode.Transaction.DateOutOfRange);
    }

    [Fact]
    public void Validate_FromDateGreaterThanToDate_ReturnsInvalidDateRange()
    {
        // arrange
        var request = new GetTransactionsRequest(
            FromDate: new DateTime(2026, 2, 1),
            ToDate: new DateTime(2026, 1, 1));

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldHaveValidationErrorFor(x => x.ToDate)
            .WithErrorCode(ValidationErrorCode.Transaction.InvalidDateRange);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_EmptyCategoryId_ReturnsCategoryRequired(string categoryId)
    {
        // arrange
        var request = new GetTransactionsRequest(
            FromDate: new DateTime(2026, 1, 1),
            ToDate: new DateTime(2026, 1, 27),
            CategoryIds: [categoryId]);

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldHaveValidationErrorFor(x => x.CategoryIds)
            .WithErrorCode(ValidationErrorCode.Transaction.CategoryRequired);
    }

    [Fact]
    public void Validate_WhenSkipIsNegative_ReturnsInvalidPagination()
    {
        // arrange
        var request = new GetTransactionsRequest(
            FromDate: new DateTime(2026, 1, 1),
            ToDate: new DateTime(2026, 1, 27),
            Skip: -1);

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldHaveValidationErrorFor(x => x.Skip!.Value)
            .WithErrorCode(ValidationErrorCode.Transaction.InvalidPagination);
    }

    [Fact]
    public void Validate_WhenTakeIsNegative_ReturnsInvalidPagination()
    {
        // arrange
        var request = new GetTransactionsRequest(
            FromDate: new DateTime(2026, 1, 1),
            ToDate: new DateTime(2026, 1, 27),
            Take: -1);

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldHaveValidationErrorFor(x => x.Take!.Value)
            .WithErrorCode(ValidationErrorCode.Transaction.InvalidPagination);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(5, 10)]
    public void Validate_WhenSkipAndTakeAreNonNegative_ReturnsNoErrors(int skip, int take)
    {
        // arrange
        var request = new GetTransactionsRequest(
            FromDate: new DateTime(2026, 1, 1),
            ToDate: new DateTime(2026, 1, 27),
            Skip: skip,
            Take: take);

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
