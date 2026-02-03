using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Configuration;

using static Google.Apis.Sheets.v4.SpreadsheetsResource.ValuesResource;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Clients;

internal sealed partial class GoogleSheetsClient(
    IOptions<GoogleSheetsOptions> options,
    SheetsService sheetsService,
    ILogger<GoogleSheetsClient> logger) : IGoogleSheetsClient
{
    private readonly GoogleSheetsOptions options = options.Value;

    public async Task<List<string>> GetFormulas(IList<string> ranges, CancellationToken ct)
    {
        LogFetchingFormulas(ranges.Count, options.SpreadsheetId);

        var request = sheetsService.Spreadsheets.Values.BatchGet(options.SpreadsheetId);
        request.Ranges = new Repeatable<string>(ranges);
        request.ValueRenderOption = BatchGetRequest.ValueRenderOptionEnum.FORMULA;
        
        var response = await request.ExecuteAsync(ct);

        var fetchedCount = response.ValueRanges?.Count ?? 0;
        LogFormulasFetched(fetchedCount);

        return response.ValueRanges?
            .Select(vr => vr.Values?[0]?[0]?.ToString() ?? string.Empty)
            .ToList() ?? [];
    }

    public async Task SendBatchUpdate(List<ValueRange> updateData, CancellationToken ct)
    {
        LogSendingBatchUpdate(updateData.Count, options.SpreadsheetId);

        var batchRequest = new BatchUpdateValuesRequest
        {
            Data = updateData,
            ValueInputOption = Utilities.ConvertToString(UpdateRequest.ValueInputOptionEnum.USERENTERED)
        };

        await sheetsService.Spreadsheets.Values
            .BatchUpdate(batchRequest, options.SpreadsheetId)
            .ExecuteAsync(ct);

        LogBatchUpdateApplied();
    }
}