using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Configuration;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Models;

using static Google.Apis.Sheets.v4.SpreadsheetsResource.ValuesResource;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Clients;

internal sealed partial class GoogleSheetsClient(
    IOptions<GoogleSheetsOptions> options,
    SheetsService sheetsService,
    ILogger<GoogleSheetsClient> logger) : IGoogleSheetsClient
{
    private readonly GoogleSheetsOptions options = options.Value;

    public async Task<IReadOnlyList<GoogleSheetCell>> GetCells(
        IEnumerable<GoogleSheetCellAddress> cellAddresses,
        CancellationToken ct)
    {
        var cellAddressesList = cellAddresses.ToList();
        LogFetchingCells(cellAddressesList.Count, options.SpreadsheetId);

        var request = sheetsService.Spreadsheets.Values.BatchGet(options.SpreadsheetId);
        request.Ranges = new Repeatable<string>(cellAddressesList.Select(ConstructRange));
        request.ValueRenderOption = BatchGetRequest.ValueRenderOptionEnum.FORMULA;

        var response = await request.ExecuteAsync(ct);
        var values = response.ValueRanges ?? [];

        LogCellsFetched(values.Count);

        return [.. cellAddressesList.Zip(
            values,
            (address, valueRange) => new GoogleSheetCell(
                address,
                valueRange.Values?[0]?[0]?.ToString() ?? string.Empty))];
    }

    public async Task SendBatchUpdate(
        IEnumerable<GoogleSheetCell> cells,
        CancellationToken ct)
    {
        var cellsList = cells.ToList();

        LogSendingBatchUpdate(cellsList.Count, options.SpreadsheetId);

        var updateData = cellsList
            .Select(cell => new ValueRange
            {
                Range = ConstructRange(cell.Address),
                Values = [[cell.Content]]
            })
            .ToList();

        var batchRequest = new BatchUpdateValuesRequest
        {
            Data = updateData,
            ValueInputOption = Utilities.ConvertToString(
                UpdateRequest.ValueInputOptionEnum.USERENTERED)
        };

        await sheetsService.Spreadsheets.Values
            .BatchUpdate(batchRequest, options.SpreadsheetId)
            .ExecuteAsync(ct);

        LogBatchUpdateApplied();
    }

    private static string ConstructRange(GoogleSheetCellAddress cellAddress)
        => $"{cellAddress.SheetName}!{cellAddress.Column}{cellAddress.Row}";
}