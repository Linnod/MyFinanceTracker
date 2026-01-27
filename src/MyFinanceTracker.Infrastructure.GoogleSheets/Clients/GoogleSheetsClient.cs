using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyFinanceTracker.Infrastructure.GoogleSheets.Configuration;
using static Google.Apis.Sheets.v4.SpreadsheetsResource.ValuesResource;

namespace MyFinanceTracker.Infrastructure.GoogleSheets.Clients;

internal class GoogleSheetsClient(IOptions<GoogleSheetsOptions> options,
    SheetsService sheetsService,
    ILogger<GoogleSheetsClient> logger) : IGoogleSheetsClient
{
    private readonly GoogleSheetsOptions options = options.Value;

    public async Task<List<string>> GetFormulas(IList<string> ranges, CancellationToken ct)
    {
        logger.LogInformation("--> GetFormulas");

        logger.LogInformation("🔍 Fetching formulas for {Count} ranges from Spreadsheet {Id}",
                    ranges.Count, options.SpreadsheetId);

        var request = sheetsService.Spreadsheets.Values.BatchGet(options.SpreadsheetId);
        request.Ranges = new Repeatable<string>(ranges);
        request.ValueRenderOption = BatchGetRequest.ValueRenderOptionEnum.FORMULA;
        var response = await request.ExecuteAsync(ct);

        logger.LogDebug("✅ Successfully fetched {Count} value ranges", response.ValueRanges?.Count ?? 0);

        var result = response.ValueRanges?
            .Select(vr => vr.Values?[0]?[0]?.ToString() ?? string.Empty)
            .ToList() ?? [];

        logger.LogInformation("<-- GetFormulas");
        
        return result;
    }

    public async Task SendBatchUpdate(List<ValueRange> updateData, CancellationToken ct)
    {
        logger.LogInformation("--> SendBatchUpdate");

        logger.LogInformation("📤 Sending batch update to Google Sheets ({Count} entries)", updateData.Count);

        var batchRequest = new BatchUpdateValuesRequest
        {
            Data = updateData,
            ValueInputOption = Utilities.ConvertToString(UpdateRequest.ValueInputOptionEnum.USERENTERED)
        };

        await sheetsService.Spreadsheets.Values
            .BatchUpdate(batchRequest, options.SpreadsheetId)
            .ExecuteAsync(ct);

        logger.LogInformation("✅ Batch update applied successfully");

        logger.LogInformation("<-- SendBatchUpdate");
    }
}