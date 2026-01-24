using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util;
using Microsoft.Extensions.Options;
using MyFinanceTracker.Infrastructure.GoogleSheets.Configuration;
using static Google.Apis.Sheets.v4.SpreadsheetsResource.ValuesResource;

namespace MyFinanceTracker.Infrastructure.GoogleSheets.Clients;

internal class GoogleSheetsClient(IOptions<GoogleSheetsOptions> options, SheetsService sheetsService)
{
    private readonly GoogleSheetsOptions options = options.Value;

    public async Task<List<string>> GetFormulasAsync(IList<string> ranges, CancellationToken ct)
    {
        var request = sheetsService.Spreadsheets.Values.BatchGet(options.SpreadsheetId);
        request.Ranges = new Repeatable<string>(ranges);
        request.ValueRenderOption = BatchGetRequest.ValueRenderOptionEnum.FORMULA;
        var response = await request.ExecuteAsync(ct);

        return response.ValueRanges?
            .Select(vr => vr.Values?[0]?[0]?.ToString() ?? string.Empty)
            .ToList() ?? [];
    }

    public async Task SendBatchUpdateAsync(List<ValueRange> updateData, CancellationToken ct)
    {
        var batchRequest = new BatchUpdateValuesRequest
        {
            Data = updateData,
            ValueInputOption = Utilities.ConvertToString(UpdateRequest.ValueInputOptionEnum.USERENTERED)
        };

        await sheetsService.Spreadsheets.Values
            .BatchUpdate(batchRequest, options.SpreadsheetId)
            .ExecuteAsync(ct);
    }
}