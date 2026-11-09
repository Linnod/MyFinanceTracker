using FluentAssertions;
using MediatR;
using MyFinanceTracker.UseCases.Behaviors;

namespace MyFinanceTracker.UseCases.Tests.Behaviors;

public class LoggingBehaviorBaseTests
{
    private readonly TestLoggingBehavior sut = new();

    [Fact]
    public async Task Handle_SuccessfulExecution_CallsOnStartingAndOnResultAndReturnsResponse()
    {
        // arrange
        var request = new TestRequest("Test");
        var expectedResponse = new TestResponse.Success("OK");
        RequestHandlerDelegate<TestResponse> next = () => Task.FromResult<TestResponse>(expectedResponse);

        // act
        var result = await sut.Handle(request, next, CancellationToken.None);

        // assert
        result.Should().Be(expectedResponse);
        sut.StartingRequests.Should().ContainSingle().Which.Should().Be(request);
        sut.Results.Should().ContainSingle().Which.Should().Be(expectedResponse);
        sut.CriticalErrors.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CancellationRequested_RethrowsExceptionWithoutCallingCriticalError()
    {
        // arrange
        var request = new TestRequest("Test");
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        RequestHandlerDelegate<TestResponse> next = () => throw new OperationCanceledException(cts.Token);

        // act
        var act = () => sut.Handle(request, next, cts.Token);

        // assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        sut.StartingRequests.Should().ContainSingle().Which.Should().Be(request);
        sut.Results.Should().BeEmpty();
        sut.CriticalErrors.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UnexpectedException_CallsOnCriticalErrorAndReturnsFailureResponse()
    {
        // arrange
        var request = new TestRequest("Test");
        var exception = new InvalidOperationException("Database failure");
        RequestHandlerDelegate<TestResponse> next = () => throw exception;

        // act
        var result = await sut.Handle(request, next, CancellationToken.None);

        // assert
        result.Should().BeOfType<TestResponse.Failure>();
        sut.StartingRequests.Should().ContainSingle().Which.Should().Be(request);
        sut.Results.Should().BeEmpty();
        sut.CriticalErrors.Should().ContainSingle().Which.Should().BeSameAs(exception);
    }

    private sealed class TestLoggingBehavior : LoggingBehaviorBase<TestRequest, TestResponse>
    {
        public List<TestRequest> StartingRequests { get; } = [];
        public List<TestResponse> Results { get; } = [];
        public List<Exception> CriticalErrors { get; } = [];

        protected override void OnStarting(TestRequest request) =>
            StartingRequests.Add(request);

        protected override void OnResult(TestResponse response) =>
            Results.Add(response);

        protected override void OnCriticalError(Exception ex) =>
            CriticalErrors.Add(ex);

        protected override TestResponse CreateFailureResponse() =>
            new TestResponse.Failure();
    }

    private sealed record TestRequest(string Name) : IRequest<TestResponse>;

    private abstract record TestResponse : UseCaseResponse
    {
        public sealed record Success(string Data) : TestResponse;
        public sealed record Failure : TestResponse, IFailureResponse;
    }
}
