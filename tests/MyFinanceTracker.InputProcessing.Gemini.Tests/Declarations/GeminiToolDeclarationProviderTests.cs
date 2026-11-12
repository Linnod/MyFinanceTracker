using FluentAssertions;
using Google.GenAI.Types;
using MyFinanceTracker.InputProcessing.Gemini.Declarations;
using Xunit;
using Type = Google.GenAI.Types.Type;

namespace MyFinanceTracker.InputProcessing.Gemini.Tests.Declarations;

public sealed class GeminiToolDeclarationProviderTests
{
    private readonly GeminiToolDeclarationProvider sut = new();

    [Fact]
    public void GetToolDeclarations_ShouldContainCreateCategoryToolWithCorrectSchema()
    {
        // arrange & act
        var declarations = sut.GetToolDeclarations();

        // assert
        declarations.Should().NotBeEmpty();
        var allFunctions = declarations.SelectMany(d => d.FunctionDeclarations ?? []).ToList();
        var createCategory = allFunctions.FirstOrDefault(f => f.Name == GeminiToolDeclarationProvider.ToolNames.CreateCategory);

        createCategory.Should().NotBeNull();
        createCategory!.Parameters.Should().NotBeNull();
        createCategory.Parameters!.Type.Should().Be(Type.Object);
        createCategory.Parameters.Properties.Should().NotBeNull();
        createCategory.Parameters.Properties!.Should().ContainKey("id");
        createCategory.Parameters.Properties!["id"].Type.Should().Be(Type.String);
        createCategory.Parameters.Properties.Should().ContainKey("name");
        createCategory.Parameters.Properties!["name"].Type.Should().Be(Type.String);
        createCategory.Parameters.Required.Should().BeEquivalentTo(["id", "name"]);
    }

    [Fact]
    public void GetToolDeclarations_ShouldContainUpdateCategoryToolWithCorrectSchema()
    {
        // arrange & act
        var declarations = sut.GetToolDeclarations();

        // assert
        declarations.Should().NotBeEmpty();
        var allFunctions = declarations.SelectMany(d => d.FunctionDeclarations ?? []).ToList();
        var updateCategory = allFunctions.FirstOrDefault(f => f.Name == GeminiToolDeclarationProvider.ToolNames.UpdateCategory);

        updateCategory.Should().NotBeNull();
        updateCategory!.Parameters.Should().NotBeNull();
        updateCategory.Parameters!.Type.Should().Be(Type.Object);
        updateCategory.Parameters.Properties.Should().NotBeNull();
        updateCategory.Parameters.Properties!.Should().ContainKey("id");
        updateCategory.Parameters.Properties!["id"].Type.Should().Be(Type.String);
        updateCategory.Parameters.Properties.Should().ContainKey("name");
        updateCategory.Parameters.Properties!["name"].Type.Should().Be(Type.String);
        updateCategory.Parameters.Properties.Should().ContainKey("isArchived");
        updateCategory.Parameters.Properties!["isArchived"].Type.Should().Be(Type.Boolean);
        updateCategory.Parameters.Required.Should().BeEquivalentTo(["id"]);
    }
}
