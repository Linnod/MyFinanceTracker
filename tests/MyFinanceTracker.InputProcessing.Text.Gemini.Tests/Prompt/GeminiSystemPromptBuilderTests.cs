using FluentAssertions;
using MediatR;
using MyFinanceTracker.InputProcessing.Text.Gemini.Prompt;
using MyFinanceTracker.Domain.Entities;
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
        builder = new GeminiSystemPromptBuilder(mediator);
    }

    [Fact]
    async Task BuildSystemInstruction_WhenCategoriesExist_ShouldIncludeCategoriesInPrompt()
    {
        // Arrange
        var categories = new List<Category>
        {
            new("cat_1", "Groceries",["food", "supermarket"], false),
            new("cat_2", "Salary", ["paycheck"], true)
        };

        mediator.Send(Arg.Any<ListCategoriesRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ListCategoriesResponse.Success(categories));

        // Act
        var result = await builder.BuildSystemInstruction(CancellationToken.None);

        // Assert
        result.Should().Contain("Groceries");
        result.Should().Contain("Salary");
        result.Should().Contain("[INCOME]");
        result.Should().Contain("[EXPENSE]");
        result.Should().Contain("food, supermarket");
    }
}