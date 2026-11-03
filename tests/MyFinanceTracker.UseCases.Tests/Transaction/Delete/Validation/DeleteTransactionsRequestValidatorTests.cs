using FluentValidation.TestHelper;
using MyFinanceTracker.UseCases;
using MyFinanceTracker.UseCases.Transaction.Delete;
using MyFinanceTracker.UseCases.Transaction.Delete.Validation;
using Xunit;

namespace MyFinanceTracker.UseCases.Tests.Transaction.Delete.Validation;

public class DeleteTransactionsRequestValidatorTests
{
    private readonly DeleteTransactionsRequestValidator sut;

    public DeleteTransactionsRequestValidatorTests()
    {
        sut = new DeleteTransactionsRequestValidator();
    }

    [Fact]
    public void Validate_RequestIsValid_ReturnsNoErrors()
    {
        // arrange
        var request = new DeleteTransactionsRequest([Guid.NewGuid(), Guid.NewGuid()]);

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenIdsIsNull_ReturnsIdRequiredError()
    {
        // arrange
        var request = new DeleteTransactionsRequest(Ids: null!);

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldHaveValidationErrorFor(x => x.Ids)
            .WithErrorCode(ValidationErrorCode.Transaction.IdRequired);
    }

    [Fact]
    public void Validate_WhenIdsIsEmpty_ReturnsIdRequiredError()
    {
        // arrange
        var request = new DeleteTransactionsRequest(Ids: []);

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldHaveValidationErrorFor(x => x.Ids)
            .WithErrorCode(ValidationErrorCode.Transaction.IdRequired);
    }

    [Fact]
    public void Validate_WhenIdsContainsGuidEmpty_ReturnsInvalidIdError()
    {
        // arrange
        var request = new DeleteTransactionsRequest([Guid.NewGuid(), Guid.Empty]);

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldHaveValidationErrorFor(x => x.Ids)
            .WithErrorCode(ValidationErrorCode.Transaction.InvalidId);
    }
}
