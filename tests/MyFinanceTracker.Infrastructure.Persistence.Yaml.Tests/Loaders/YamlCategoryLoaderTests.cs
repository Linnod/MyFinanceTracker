using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Configuration;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Loaders;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Loaders.Exceptions;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Parsers;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Parsers.Exceptions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MyFinanceTracker.Infrastructure.Persistence.Yaml.Tests.Loaders;

public class YamlCategoryLoaderTests
{
    private readonly ICategoryParser parser = Substitute.For<ICategoryParser>();

    private YamlCategoryLoader CreateSut(string filePath)
    {
        var options = Options.Create(new YamlPersistenceOptions { FilePath = filePath });
        return new YamlCategoryLoader(options, parser, NullLogger<YamlCategoryLoader>.Instance);
    }

    [Fact]
    void Load_ShouldThrowFileNotFound_WhenFileDoesNotExist()
    {
        // arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid()}.yaml");
        var sut = CreateSut(nonExistentPath);

        // act
        var act = () => sut.Load();

        // assert
        act.Should().Throw<CategoryLoaderException>()
            .WithMessage("*Category file not found*");
    }

    [Fact]
    void Load_ShouldReturnParsedCategories_WhenFileExists()
    {
        // arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "dummy content");
            Category[] expected = [new Category("cat1", "Category 1", ["c1"])];
            parser.Parse("dummy content").Returns(expected);

            var sut = CreateSut(tempFile);

            // act
            var result = sut.Load();

            // assert
            result.Should().BeEquivalentTo(expected);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    void Load_ShouldRethrowCategoryParserException_WhenParserThrows()
    {
        // arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "invalid content");
            parser.Parse("invalid content").Throws(CategoryParserException.NoCategoriesFound());

            var sut = CreateSut(tempFile);

            // act
            var act = () => sut.Load();

            // assert
            act.Should().Throw<CategoryParserException>()
                .WithMessage("*contains no categories*");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
