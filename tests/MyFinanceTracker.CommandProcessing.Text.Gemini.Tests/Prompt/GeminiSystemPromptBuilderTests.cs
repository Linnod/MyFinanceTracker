using FluentAssertions;
using MediatR;
using MyFinanceTracker.CommandProcessing.Text.Gemini.Prompt;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.UseCases.Category.List;
using NSubstitute;
using Xunit;

namespace MyFinanceTracker.CommandProcessing.Text.Gemini.Tests.Services;

public class GeminiSystemPromptBuilderTests
{
    private readonly IMediator mediator = Substitute.For<IMediator>();
    private readonly GeminiSystemPromptBuilder builder;

    public GeminiSystemPromptBuilderTests()
    {
        builder = new GeminiSystemPromptBuilder(mediator);
    }

    [Fact]
    public async Task BuildSystemInstructionAsync_WhenCategoriesExist_ShouldIncludeCategoriesInPrompt()
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
        var result = await builder.BuildSystemInstructionAsync();

        // Assert
        result.Should().Contain("Groceries");
        result.Should().Contain("Salary");
        result.Should().Contain("[INCOME]");
        result.Should().Contain("[EXPENSE]");
        result.Should().Contain("food, supermarket");
    }
}