using FluentAssertions;
using MediatR;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.InputProcessing.Text.Gemini.Prompt;
using MyFinanceTracker.UseCases.Category.List;
using NSubstitute;
using Xunit;

namespace MyFinanceTracker.InputProcessing.Text.Gemini.Tests.Prompt;

public class GeminiSystemPromptBuilderTests
{
    private readonly IMediator mediator = Substitute.For<IMediator>();
    private readonly GeminiSystemPromptBuilder builder;

    public GeminiSystemPromptBuilderTests()
    {
        builder = new GeminiSystemPromptBuilder(mediator, TimeProvider.System);
    }

    [Fact]
    async Task BuildSystemInstruction_WhenCategoriesExist_ShouldIncludeCategoriesInPrompt()
    {
        // Arrange
        var categories = new List<Category>
        {
            new("cat_1", "Groceries", ["food", "supermarket"]),
            new("cat_2", "Salary", ["paycheck"])
        };

        mediator.Send(Arg.Any<ListCategoriesRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ListCategoriesResponse.Success(categories));

        // Act
        var result = await builder.BuildSystemInstruction(CancellationToken.None);

        // Assert
        result.Should().Contain("Groceries");
        result.Should().Contain("Salary");
        result.Should().Contain("food, supermarket");
    }

    [Fact]
    async Task BuildSystemInstruction_ShouldIncludeNoteRules()
    {
        // Arrange
        mediator.Send(Arg.Any<ListCategoriesRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ListCategoriesResponse.Success([]));

        // Act
        var result = await builder.BuildSystemInstruction(CancellationToken.None);

        // Assert
        result.Should().Contain("`note`");
        result.Should().Contain("clarification, description, store name, service name, or occasion");
        result.Should().Contain("ALWAYS use the primary category for `categoryAlias`");
        result.Should().Contain("ALWAYS place the activity, occasion, store name, or clarification into `note`");
        result.Should().Contain("receipt image or banking app screenshot");
        result.Should().Contain("NEVER duplicate the category name into `note`");
    }
}
