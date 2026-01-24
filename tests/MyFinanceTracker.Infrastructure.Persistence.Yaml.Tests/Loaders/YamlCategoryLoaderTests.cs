using FluentAssertions;
using Microsoft.Extensions.Options;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Configuration;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Loaders;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Loaders.Exceptions;

namespace MyFinanceTracker.Infrastructure.Persistence.Yaml.Tests.Loaders;

public class YamlCategoryLoaderTests : IDisposable
{
    private readonly string _testFileName = $"categories_{Guid.NewGuid()}.yaml";

    public void Dispose()
    {
        if (File.Exists(_testFileName))
        {
            File.Delete(_testFileName);
        }

        GC.SuppressFinalize(this);
    }

    private YamlCategoryLoader CreateSut()
    {
        var options = Options.Create(new YamlPersistenceOptions
        {
            FilePath = _testFileName
        });

        return new YamlCategoryLoader(options);
    }

    [Fact]
    public void Load_ShouldThrowFileNotFound_WhenFileDoesNotExist()
    {
        // arrange
        var sut = CreateSut();

        // act
        var act = () =>
        {
            return sut.Load();
        };

        // assert
        act.Should().Throw<CategoryLoaderException>()
            .WithMessage("*Category file not found*");
    }

    [Fact]
    public void Load_ShouldThrowDuplicateId_WhenIdsAreNotUnique()
    {
        // arrange
        // Используем явные переносы строк без скрытых табов
        var yaml = "categories:\n" +
                   "  - id: A\n" +
                   "    name: First\n" +
                   "  - id: A\n" +
                   "    name: Second";

        File.WriteAllText(_testFileName, yaml);
        var sut = CreateSut();

        // act
        var act = () =>
        {
            return sut.Load();
        };

        // assert
        act.Should().Throw<CategoryLoaderException>()
            .WithMessage("*Duplicate Category ID*A*");
    }

    [Fact]
    public void Load_ShouldThrowDuplicateAlias_WhenAliasesAreNotUnique()
    {
        // arrange
        var yaml = "categories:\n" +
                   "  - id: A\n" +
                   "    name: Food\n" +
                   "    aliases: [test]\n" +
                   "  - id: B\n" +
                   "    name: Drinks\n" +
                   "    aliases: [test]";

        File.WriteAllText(_testFileName, yaml);
        var sut = CreateSut();

        // act
        var act = () =>
        {
            return sut.Load();
        };

        // assert
        act.Should().Throw<CategoryLoaderException>()
            .WithMessage("*Duplicate alias*test*");
    }

    [Fact]
    public void Load_ShouldThrowDeserializationFailed_WhenYamlIsInvalid()
    {
        // arrange
        File.WriteAllText(_testFileName, "invalid: [ [ [ yaml structure");
        var sut = CreateSut();

        // act
        var act = () =>
        {
            return sut.Load();
        };

        // assert
        act.Should().Throw<CategoryLoaderException>()
            .WithMessage("*Failed to deserialize*");
    }

    [Fact]
    public void Load_ShouldReturnCategories_WhenYamlIsValid()
    {
        // arrange
        var yaml = "categories:\n" +
                   "  - id: FOOD\n" +
                   "    name: Products\n" +
                   "    isIncome: false\n" +
                   "    aliases: [eat, meal]";

        File.WriteAllText(_testFileName, yaml);
        var sut = CreateSut();

        // act
        var result = sut.Load();

        // assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be("FOOD");
        result[0].Name.Should().Be("Products");
        result[0].Aliases.Should().Contain("eat");
    }
}