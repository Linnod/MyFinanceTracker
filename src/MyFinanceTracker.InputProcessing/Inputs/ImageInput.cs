namespace MyFinanceTracker.InputProcessing.Inputs;

public sealed record ImageInput(Stream Stream, string ContentType, string? Caption = null) : IInput
{
    public override string ToString()
    {
        var captionPart = string.IsNullOrWhiteSpace(Caption) ? string.Empty : $", caption: '{Caption}'";
        return Stream.CanSeek
            ? $"Image ({ContentType}, {Stream.Length} bytes{captionPart})"
            : $"Image ({ContentType}{captionPart})";
    }

    public void Dispose()
    {
        Stream.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await Stream.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
