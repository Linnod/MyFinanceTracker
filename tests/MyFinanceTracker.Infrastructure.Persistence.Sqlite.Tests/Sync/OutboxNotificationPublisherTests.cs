using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Common.Notifications;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Logging;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Sync;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Tests.Sync;

public sealed class OutboxNotificationPublisherTests
{
    private readonly IPublisher publisher = Substitute.For<IPublisher>();
    private readonly ILogger<OutboxNotificationPublisher> logger = Substitute.For<ILogger<OutboxNotificationPublisher>>();
    private readonly OutboxNotificationPublisher sut;

    public OutboxNotificationPublisherTests()
    {
        sut = new OutboxNotificationPublisher(publisher, logger);
    }

    [Fact]
    public async Task Publish_ValidNotification_PublishesToInnerPublisher()
    {
        // arrange
        var notification = new OutboxSyncNotification.SyncFailed("Sync failed message");
        using var cts = new CancellationTokenSource();

        // act
        await sut.Publish(notification, cts.Token);

        // assert
        await publisher.Received(1).Publish(notification, cts.Token);
    }

    [Fact]
    public async Task Publish_PublisherThrowsException_SwallowsExceptionAndLogsError()
    {
        // arrange
        logger.IsEnabled(LogLevel.Error).Returns(true);
        var notification = new OutboxSyncNotification.SyncFailed("Sync failed message");
        publisher.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("MediatR dispatch failure"));

        // act
        var act = () => sut.Publish(notification, CancellationToken.None);

        // assert
        await act.Should().NotThrowAsync();
        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Is<EventId>(e => e.Id == LogEvents.Sync.NotificationPublishFailed),
            Arg.Any<Arg.AnyType>(),
            Arg.Any<InvalidOperationException>(),
            Arg.Any<Func<Arg.AnyType, Exception?, string>>());
    }

    [Fact]
    public async Task Publish_OperationCanceledAndTokenCancelled_RethrowsException()
    {
        // arrange
        var notification = new OutboxSyncNotification.SyncFailed("Sync failed message");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        publisher.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Throws(new OperationCanceledException(cts.Token));

        // act
        var act = () => sut.Publish(notification, cts.Token);

        // assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
