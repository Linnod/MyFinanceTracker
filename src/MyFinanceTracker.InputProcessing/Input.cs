namespace MyFinanceTracker.InputProcessing;

public abstract record Input : IAsyncDisposable, IDisposable
{
    private Input() { }

    public abstract void Dispose();
    public abstract ValueTask DisposeAsync();

    public sealed record Text(string Content) : Input
    {
        public override string ToString() => Content;

        public override void Dispose() => GC.SuppressFinalize(this);

        public override ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }
    }

    public sealed record Audio(Stream Stream, string ContentType) : Input
    {
        public override string ToString() =>
            Stream.CanSeek
                ? $"Audio ({ContentType}, {Stream.Length} bytes)"
                : $"Audio ({ContentType})";

        public override void Dispose()
        {
            Stream.Dispose();
            GC.SuppressFinalize(this);
        }

        public override async ValueTask DisposeAsync()
        {
            await Stream.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }

    public sealed record Image(Stream Stream, string ContentType, string? Caption = null) : Input
    {
        public override string ToString()
        {
            var captionPart = string.IsNullOrWhiteSpace(Caption) ? string.Empty : $", caption: '{Caption}'";
            return Stream.CanSeek
                ? $"Image ({ContentType}, {Stream.Length} bytes{captionPart})"
                : $"Image ({ContentType}{captionPart})";
        }

        public override void Dispose()
        {
            Stream.Dispose();
            GC.SuppressFinalize(this);
        }

        public override async ValueTask DisposeAsync()
        {
            await Stream.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }
}
