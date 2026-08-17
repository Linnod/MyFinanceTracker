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
    public void Validate_RequestIsValid_ReturnsNoErrors()
    {
        // arrange
        var request = new GetTransactionsRequest(
            "food",
            new DateOnly(2026, 1, 27));

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    void Validate_CategoryAliasIsEmpty_ReturnsRequiredError(string alias)
    {
        // arrange
        var request = new GetTransactionsRequest(
            alias,
            new DateOnly(2026, 1, 27));

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldHaveValidationErrorFor(x => x.CategoryAlias)
            .WithErrorCode(ValidationErrorCode.Common.Required);
    }

    [Fact]
    void Validate_DateIsNull_ReturnsRequiredError()
    {
        // arrange
        var request = new GetTransactionsRequest(
            "food",
            null);

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldHaveValidationErrorFor(x => x.Date)
            .WithErrorCode(ValidationErrorCode.Common.Required);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void Validate_DateOutOfRange_ReturnsError(int offset)
    {
        // arrange
        var year = offset < 0
            ? FinancialRules.MinAllowedYear - 1
            : FinancialRules.MaxAllowedYear + 1;

        var request = new GetTransactionsRequest(
            "food",
            new DateOnly(year, 1, 1));

        // act
        var result = sut.TestValidate(request);

        // assert
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(GetTransactionsRequest.Date) + ".Value.Year" &&
            e.ErrorCode == ValidationErrorCode.Common.DateOutOfRange);
    }
}