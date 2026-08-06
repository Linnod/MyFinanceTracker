using System.Text.Json.Serialization;
using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.Interactions.Api.Dtos;

public record CreateTransactionDto(
    [property: JsonConverter(typeof(JsonStringEnumConverter))] TransactionType Type,
    decimal[] Amounts,
    string? CategoryAlias = null,
    DateOnly? Date = null,
    string? Note = null
);