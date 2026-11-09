using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using MyFinanceTracker.UseCases.Behaviors;
using NSubstitute;

namespace MyFinanceTracker.UseCases.Tests.Behaviors;

public class ValidationBehaviorTests
{
    private readonly IValidator<TestRequest> validator = Substitute.For<IValidator<TestRequest>>();
    private readonly List<IReadOnlyList<ValidationErrorItem>> createdErrorBatches = [];
    private readonly ValidationBehavior<TestRequest, TestResponse> sut;

    public ValidationBehaviorTests()
    {
        sut = new ValidationBehavior<TestRequest, TestResponse>(
            validator,
            errors =>
            {
                createdErrorBatches.Add(errors);
                return new TestResponse.ValidationError(errors);
            });
    }

    [Fact]
    public async Task Handle_WhenRequestIsValid_CallsNextAndReturnsResponse()
    {
        // arrange
        var request = new TestRequest("valid");
        var expectedResponse = new TestResponse.Success("OK");
        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult<TestResponse>(expectedResponse);

        validator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        // act
        var result = await sut.Handle(request, next, CancellationToken.None);

        // assert
        result.Should().Be(expectedResponse);
        createdErrorBatches.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenRequestIsInvalid_ReturnsValidationErrorWithoutCallingNext()
    {
        // arrange
        var request = new TestRequest("invalid");
        var validationFailures = new List<ValidationFailure>
        {
            new("Name", "Name is required") { ErrorCode = "NAME_REQUIRED", AttemptedValue = "invalid" }
        };

        validator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(validationFailures));

        var nextCalled = false;
        RequestHandlerDelegate<TestResponse> next = () =>
        {
            nextCalled = true;
            return Task.FromResult<TestResponse>(new TestResponse.Success("OK"));
        };

        // act
        var result = await sut.Handle(request, next, CancellationToken.None);

        // assert
        nextCalled.Should().BeFalse();
        var validationError = result.Should().BeOfType<TestResponse.ValidationError>().Subject;
        validationError.Errors.Should().ContainSingle();
        validationError.Errors.First().ErrorCode.Should().Be("NAME_REQUIRED");
        validationError.Errors.First().AttemptedValue.Should().Be("invalid");
        createdErrorBatches.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_CancellationRequested_PassesCancellationTokenToValidator()
    {
        // arrange
        var request = new TestRequest("valid");
        using var cts = new CancellationTokenSource();
        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult<TestResponse>(new TestResponse.Success("OK"));

        validator.ValidateAsync(request, cts.Token)
            .Returns(new ValidationResult());

        // act
        await sut.Handle(request, next, cts.Token);

        // assert
        await validator.Received(1).ValidateAsync(request, cts.Token);
    }

    public sealed record TestRequest(string Name) : IRequest<TestResponse>;

    public abstract record TestResponse : UseCaseResponse
    {
        public sealed record Success(string Data) : TestResponse;

        public sealed record ValidationError(IReadOnlyCollection<ValidationErrorItem> Errors)
            : TestResponse, IValidationErrorResponse;
    }
}
