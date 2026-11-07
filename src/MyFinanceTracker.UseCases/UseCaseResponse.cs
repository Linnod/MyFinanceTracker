namespace MyFinanceTracker.UseCases;

public abstract record UseCaseResponse;

public interface IValidationErrorResponse
{
    IReadOnlyCollection<ValidationErrorItem> Errors { get; }
}

public interface IFailureResponse;
