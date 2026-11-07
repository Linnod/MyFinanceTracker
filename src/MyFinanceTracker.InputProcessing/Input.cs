using MediatR;

namespace MyFinanceTracker.InputProcessing;

public abstract record Input : IRequest<ProcessingResult>
{
    private Input() { }

    public sealed record Text(string Content) : Input
    {
        public override string ToString() => Content;
    }

    public sealed record Audio(byte[] Data, string ContentType) : Input
    {
        public override string ToString() => $"Audio ({ContentType}, {Data.Length} bytes)";
    }

    public sealed record Image(byte[] Data, string ContentType, string? Caption = null) : Input
    {
        public override string ToString()
        {
            var captionPart = string.IsNullOrWhiteSpace(Caption) ? string.Empty : $", caption: '{Caption}'";
            return $"Image ({ContentType}, {Data.Length} bytes{captionPart})";
        }
    }
}
