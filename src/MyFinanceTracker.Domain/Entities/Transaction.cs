using MyFinanceTracker.Domain.Exceptions;

namespace MyFinanceTracker.Domain.Entities;

public record Transaction
{
    public Guid Id { get; }
    public TransactionType Type { get; }
    public Category Category { get; }
    public decimal Amount { get; }
    public DateOnly Date { get; }
    public string? Note { get; }

    public Transaction(
        Guid id,
        TransactionType type,
        Category category,
        decimal amount,
        DateOnly date,
        string? note)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Transaction ID cannot be empty.");
        }

        if (category is null)
        {
            throw new DomainException("Transaction category is required.");
        }

        if (amount == 0)
        {
            throw new DomainException("Transaction amount cannot be zero.");
        }

        if (date.Year < FinancialRules.MinAllowedYear || date.Year > FinancialRules.MaxAllowedYear)
        {
            throw new DomainException($"Transaction date year must be between {FinancialRules.MinAllowedYear} and {FinancialRules.MaxAllowedYear}.");
        }

        Id = id;
        Type = type;
        Category = category;
        Amount = type == TransactionType.Expense ? -Math.Abs(amount) : Math.Abs(amount);
        Date = date;
        Note = note;
    }
}