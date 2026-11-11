using FluentAssertions;
using MediatR;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.InputProcessing.Gemini.Prompt;
using MyFinanceTracker.UseCases.Category.List;
using NSubstitute;
using Xunit;

namespace MyFinanceTracker.InputProcessing.Gemini.Tests.Prompt;

public class GeminiSystemPromptBuilderTests
{
    private readonly ISender sender = Substitute.For<ISender>();
    private readonly GeminiSystemPromptBuilder builder;

    public GeminiSystemPromptBuilderTests()
    {
        builder = new GeminiSystemPromptBuilder(sender, TimeProvider.System);
    }

    [Fact]
    async Task BuildSystemInstruction_WhenCategoriesExist_ShouldIncludeCategoriesInPrompt()
    {
        // arrange
        var categories = new List<Category>
        {
            new("cat_1", "Groceries"),
            new("cat_2", "Salary")
        };

        sender.Send(Arg.Any<ListCategoriesRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ListCategoriesResponse.Success(categories));

        // act
        var result = await builder.BuildSystemInstruction(CancellationToken.None);

        // assert
        result.Should().Contain("Groceries");
        result.Should().Contain("Salary");
        result.Should().Contain("cat_1");
        result.Should().Contain("cat_2");
    }

    [Fact]
    async Task BuildSystemInstruction_WhenCategoriesEmptyOrFails_ShouldIncludeFallbackMessage()
    {
        // arrange
        sender.Send(Arg.Any<ListCategoriesRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ListCategoriesResponse.Failure());

        // act
        var result = await builder.BuildSystemInstruction(CancellationToken.None);

        // assert
        result.Should().Contain("No categories available.");
    }
}
