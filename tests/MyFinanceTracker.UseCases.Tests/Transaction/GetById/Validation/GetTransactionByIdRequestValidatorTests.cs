using FluentAssertions;
using FluentValidation.TestHelper;
using MyFinanceTracker.UseCases.Transaction.GetById;
using MyFinanceTracker.UseCases.Transaction.GetById.Validation;

namespace MyFinanceTracker.UseCases.Tests.Transaction.GetById.Validation;

public sealed class GetTransactionByIdRequestValidatorTests
{
    private readonly GetTransactionByIdRequestValidator sut = new();

    [Fact]
    public void Validate_EmptyId_ReturnsInvalidIdError()
    {
        // arrange
        var request = new GetTransactionByIdRequest(Guid.Empty);

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorCode(ValidationErrorCode.Transaction.InvalidId);
    }

    [Fact]
    public void Validate_ValidId_ReturnsNoErrors()
    {
        // arrange
        var request = new GetTransactionByIdRequest(Guid.NewGuid());

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
