namespace MyFinanceTracker.InputProcessing.Inputs;

public sealed record AudioInput(Stream Stream, string ContentType) : IInput
{
    public override string ToString() =>
        Stream.CanSeek
            ? $"Audio ({ContentType}, {Stream.Length} bytes)"
            : $"Audio ({ContentType})";

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