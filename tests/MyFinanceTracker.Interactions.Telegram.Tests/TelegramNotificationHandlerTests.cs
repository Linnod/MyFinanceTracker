using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyFinanceTracker.Common.Notifications;
using MyFinanceTracker.Interactions.Telegram.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Telegram.Bot;
using Telegram.Bot.Requests;

namespace MyFinanceTracker.Interactions.Telegram.Tests;

public sealed class TelegramNotificationHandlerTests
{
    private const long AllowedUserId = 123456789;

    private readonly ITelegramBotClient botClient = Substitute.For<ITelegramBotClient>();
    private readonly ILogger<TelegramNotificationHandler> logger = Substitute.For<ILogger<TelegramNotificationHandler>>();
    private readonly TelegramNotificationHandler sut;

    public TelegramNotificationHandlerTests()
    {
        var options = Options.Create(new TelegramInteractionOptions
        {
            AllowedUserId = AllowedUserId,
            Token = "test-token"
        });

        sut = new TelegramNotificationHandler(botClient, options, logger);
    }

    [Fact]
    public async Task Handle_MessageMovedToDeadLetter_SendsTelegramMessageToAllowedUser()
    {
        // arrange
        var notification = new OutboxSyncNotification.MessageMovedToDeadLetter(10, "Create", "Error");
        using var cts = new CancellationTokenSource();

        // act
        await sut.Handle(notification, cts.Token);

        // assert
        await botClient.Received(1).SendRequest(
            Arg.Is<SendMessageRequest>(r => r.ChatId.Identifier == AllowedUserId),
            cts.Token);
    }

    [Fact]
    public async Task Handle_SyncFailed_SendsTelegramMessageToAllowedUser()
    {
        // arrange
        var notification = new OutboxSyncNotification.SyncFailed("Sync error");
        using var cts = new CancellationTokenSource();

        // act
        await sut.Handle(notification, cts.Token);

        // assert
        await botClient.Received(1).SendRequest(
            Arg.Is<SendMessageRequest>(r => r.ChatId.Identifier == AllowedUserId),
            cts.Token);
    }

    [Fact]
    public async Task Handle_BotClientThrowsException_SwallowsExceptionAndLogsError()
    {
        // arrange
        logger.IsEnabled(LogLevel.Error).Returns(true);
        var notification = new OutboxSyncNotification.SyncFailed("Sync error");
        botClient.SendRequest(Arg.Any<SendMessageRequest>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Telegram API failure"));

        // act
        var act = () => sut.Handle(notification, CancellationToken.None);

        // assert
        await act.Should().NotThrowAsync();
        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Is<EventId>(e => e.Id == LogEvents.Notifications.NotificationSendFailed),
            Arg.Any<Arg.AnyType>(),
            Arg.Any<InvalidOperationException>(),
            Arg.Any<Func<Arg.AnyType, Exception?, string>>());
    }

    [Fact]
    public async Task Handle_OperationCanceledAndTokenCancelled_RethrowsException()
    {
        // arrange
        var notification = new OutboxSyncNotification.SyncFailed("Sync error");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        botClient.SendRequest(Arg.Any<SendMessageRequest>(), Arg.Any<CancellationToken>())
            .Throws(new OperationCanceledException(cts.Token));

        // act
        var act = () => sut.Handle(notification, cts.Token);

        // assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
