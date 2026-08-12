using FluentAssertions;
using Microsoft.Extensions.Options;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Configuration;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Loaders;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Loaders.Exceptions;

namespace MyFinanceTracker.Infrastructure.Persistence.Yaml.Tests.Loaders;

public class YamlCategoryLoaderTests : IDisposable
{
    private readonly string testFileName = $"categories_{Guid.NewGuid()}.yaml";

    public void Dispose()
    {
        if (File.Exists(testFileName))
        {
            File.Delete(testFileName);
        }
        GC.SuppressFinalize(this);
    }

    private YamlCategoryLoader CreateSut()
    {
        var options = Options.Create(new YamlPersistenceOptions { FilePath = testFileName });
        return new YamlCategoryLoader(options);
    }

    private void WriteYaml(string content) => File.WriteAllText(testFileName, content);

    [Fact]
    void Load_ShouldThrowFileNotFound_WhenFileDoesNotExist()
    {
        var sut = CreateSut();
        var act = () => sut.Load();

        act.Should().Throw<CategoryLoaderException>()
            .WithMessage("*Category file not found*");
    }

    [Fact]
    void Load_ShouldThrowDefaultIncomeCategoryMissing_WhenIncomeIsMissing()
    {
        // arrange:
        WriteYaml("""
            categories:
              - id: FOOD
                name: Products
                isIncome: false
                aliases: [eat, meal]
            """);
        var sut = CreateSut();

        // act
        var act = () => sut.Load();

        // assert
        act.Should().Throw<CategoryLoaderException>()
            .WithMessage($"*Required default income category with alias '{FinancialRules.DefaultIncomeCategoryAlias}' is missing*");
    }

    [Fact]
    void Load_ShouldThrowDuplicateId_WhenIdsAreNotUnique()
    {
        // arrange:
        WriteYaml($"""
            categories:
              - id: {FinancialRules.DefaultIncomeCategoryAlias}
                name: Income
                isIncome: true
                aliases: [{FinancialRules.DefaultIncomeCategoryAlias}]
              - id: A
                name: First
                aliases: [first]
              - id: A
                name: Second
                aliases: [second]
            """);
        var sut = CreateSut();

        // act
        var act = () => sut.Load();

        // assert
        act.Should().Throw<CategoryLoaderException>()
            .WithMessage("*Duplicate Category ID*A*");
    }

    [Fact]
    void Load_ShouldThrowDuplicateAlias_WhenAliasesAreNotUnique()
    {
        // arrange:
        WriteYaml($"""
            categories:
              - id: A
                name: Income
                isIncome: true
                aliases: [{FinancialRules.DefaultIncomeCategoryAlias}]
              - id: B
                name: Food
                aliases: [test]
              - id: C
                name: Drinks
                aliases: [test]
            """);
        var sut = CreateSut();

        // act
        var act = () => sut.Load();

        // assert
        act.Should().Throw<CategoryLoaderException>()
            .WithMessage("*Duplicate alias*test*");
    }

    [Fact]
    void Load_ShouldReturnCategories_WhenYamlIsValid()
    {
        // arrange
        WriteYaml($"""
            categories:
              - id: INCOME
                name: Salary
                isIncome: true
                aliases: [{FinancialRules.DefaultIncomeCategoryAlias}]
              - id: FOOD
                name: Products
                isIncome: false
                aliases: [eat, meal]
            """);
        var sut = CreateSut();

        // act
        var result = sut.Load();

        // assert
        result.Should().HaveCount(2);
        
        result.Should().ContainSingle(c => 
            c.Id == "INCOME" && 
            c.IsIncome && 
            c.Aliases.Contains(FinancialRules.DefaultIncomeCategoryAlias));

        result.Should().ContainSingle(c => 
            c.Id == "FOOD" && 
            c.Aliases.Contains("eat"));
    }

    [Fact]
    void Load_ShouldThrowDeserializationFailed_WhenYamlIsInvalid()
    {
        //arrange
        WriteYaml("invalid: [ [ [ yaml structure");
        var sut = CreateSut();

        //act
        var act = () => sut.Load();

        //assert
        act.Should().Throw<CategoryLoaderException>()
            .WithMessage("*Failed to deserialize*");
    }
}