using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Options;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Tests;

public class SqlitePersistenceOptionsTests
{
    [Fact]
    public void DefaultValues_ShouldBeValid()
    {
        // Arrange
        var options = new SqlitePersistenceOptions();
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(options);

        // Act
        var isValid = Validator.TryValidateObject(options, context, validationResults, true);

        // Assert
        isValid.Should().BeTrue();
        options.ConnectionString.Should().Be("Data Source=finance.db");
        options.SyncIntervalSeconds.Should().Be(60);
        options.MaxRetryAttempts.Should().Be(5);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    [InlineData(-1)]
    public void MaxRetryAttempts_OutOfRange_ShouldBeInvalid(int invalidMaxRetries)
    {
        // Arrange
        var options = new SqlitePersistenceOptions
        {
            MaxRetryAttempts = invalidMaxRetries
        };
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(options);

        // Act
        var isValid = Validator.TryValidateObject(options, context, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().Contain(r => r.MemberNames.Contains(nameof(SqlitePersistenceOptions.MaxRetryAttempts)));
    }

    [Theory]
    [InlineData(4)]
    [InlineData(3601)]
    [InlineData(-1)]
    public void SyncIntervalSeconds_OutOfRange_ShouldBeInvalid(int invalidInterval)
    {
        // Arrange
        var options = new SqlitePersistenceOptions
        {
            SyncIntervalSeconds = invalidInterval
        };
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(options);

        // Act
        var isValid = Validator.TryValidateObject(options, context, validationResults, true);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().Contain(r => r.MemberNames.Contains(nameof(SqlitePersistenceOptions.SyncIntervalSeconds)));
    }
}
