using MyFinanceTracker.Common.Notifications;
using NSubstitute;

namespace MyFinanceTracker.Interactions.Telegram.Tests;

public sealed class TelegramNotificationHandlerTests
{
    private readonly ITelegramMessageSender messageSender = Substitute.For<ITelegramMessageSender>();
    private readonly TelegramNotificationHandler sut;

    public TelegramNotificationHandlerTests()
    {
        sut = new TelegramNotificationHandler(messageSender);
    }

    [Fact]
    public async Task Handle_MessageMovedToDeadLetter_SendsFormattedNotification()
    {
        // arrange
        var notification = new OutboxSyncNotification.MessageMovedToDeadLetter(10, "Create", "Error");
        using var cts = new CancellationTokenSource();

        // act
        await sut.Handle(notification, cts.Token);

        // assert
        await messageSender.Received(1).Send(
            TelegramResponseFormatter.FormatNotification(notification),
            cts.Token);
    }

    [Fact]
    public async Task Handle_SyncFailed_SendsFormattedNotification()
    {
        // arrange
        var notification = new OutboxSyncNotification.SyncFailed("Sync error");
        using var cts = new CancellationTokenSource();

        // act
        await sut.Handle(notification, cts.Token);

        // assert
        await messageSender.Received(1).Send(
            TelegramResponseFormatter.FormatNotification(notification),
            cts.Token);
    }
}
