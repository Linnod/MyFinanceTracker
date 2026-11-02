using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
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

    public async Task<IReadOnlyList<GoogleSheetRow>> GetRows(string sheetName, CancellationToken ct)
    {
        LogFetchingRows(sheetName, options.SpreadsheetId);

        var request = sheetsService.Spreadsheets.Values.Get(options.SpreadsheetId, sheetName);
        request.ValueRenderOption = GetRequest.ValueRenderOptionEnum.FORMATTEDVALUE;

        var response = await request.ExecuteAsync(ct);
        var rows = response.Values ?? [];

        LogRowsFetched(rows.Count, sheetName);

        return [.. rows.Select((row, index) => new GoogleSheetRow(
            index,
            new GoogleSheetRowValues([.. row.Select(cell => cell?.ToString() ?? string.Empty)])))];
    }

    public async Task AppendRows(string sheetName, IEnumerable<GoogleSheetRowValues> rows, CancellationToken ct)
    {
        var rowsList = rows.ToList();
        if (rowsList.Count == 0)
        {
            return;
        }

        LogAppendingRows(rowsList.Count, sheetName, options.SpreadsheetId);

        var valueRange = new ValueRange
        {
            Values = [.. rowsList.Select(r => (IList<object>)[.. r.Values])]
        };

        var appendRequest = sheetsService.Spreadsheets.Values.Append(valueRange, options.SpreadsheetId, sheetName);
        appendRequest.ValueInputOption = AppendRequest.ValueInputOptionEnum.USERENTERED;
        appendRequest.InsertDataOption = AppendRequest.InsertDataOptionEnum.OVERWRITE;

        await appendRequest.ExecuteAsync(ct);

        LogRowsAppended(sheetName);
    }

    public async Task DeleteRows(string sheetName, IReadOnlyCollection<GoogleSheetRow> rows, CancellationToken ct)
    {
        if (rows.Count == 0)
        {
            return;
        }

        LogDeletingRows(rows.Count, sheetName, options.SpreadsheetId);

        var sheetId = await GetSheetId(sheetName, ct);

        var requests = rows
            .Select(r => r.Index)
            .OrderByDescending(i => i)
            .Select(i => new Request
            {
                DeleteDimension = new DeleteDimensionRequest
                {
                    Range = new DimensionRange
                    {
                        SheetId = sheetId,
                        Dimension = "ROWS",
                        StartIndex = i,
                        EndIndex = i + 1
                    }
                }
            })
            .ToList();

        var batchUpdateRequest = new BatchUpdateSpreadsheetRequest
        {
            Requests = requests
        };

        await sheetsService.Spreadsheets
            .BatchUpdate(batchUpdateRequest, options.SpreadsheetId)
            .ExecuteAsync(ct);

        LogRowsDeleted(sheetName);
    }

    private async Task<int> GetSheetId(string sheetName, CancellationToken ct)
    {
        var spreadsheet = await sheetsService.Spreadsheets
            .Get(options.SpreadsheetId)
            .ExecuteAsync(ct);

        var sheet = spreadsheet.Sheets?.FirstOrDefault(s =>
            string.Equals(s.Properties.Title, sheetName, StringComparison.OrdinalIgnoreCase));

        if (sheet?.Properties?.SheetId == null)
        {
            throw new InvalidOperationException(
                $"Sheet '{sheetName}' not found in spreadsheet '{options.SpreadsheetId}'.");
        }

        return sheet.Properties.SheetId.Value;
    }
}
