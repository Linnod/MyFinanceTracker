using MediatR;

namespace MyFinanceTracker.InputProcessing;

public closed record Input(string? Context = null) : IRequest<ProcessingResult>
{
    public sealed record Text(string Content, string? Context = null) : Input(Context)
    {
        public override string ToString() => $"{Content}{FormatContext(Context)}";
    }

    public sealed record Audio(byte[] Data, string ContentType, string? Caption = null, string? Context = null) : Input(Context)
    {
        public override string ToString() => $"Audio ({ContentType}, {Data.Length} bytes{FormatCaption(Caption)}{FormatContext(Context)})";
    }

    public sealed record Image(byte[] Data, string ContentType, string? Caption = null, string? Context = null) : Input(Context)
    {
        public override string ToString() => $"Image ({ContentType}, {Data.Length} bytes{FormatCaption(Caption)}{FormatContext(Context)})";
    }

    public sealed record Document(byte[] Data, string ContentType, string? Caption = null, string? Context = null) : Input(Context)
    {
        public override string ToString() => $"Document ({ContentType}, {Data.Length} bytes{FormatCaption(Caption)}{FormatContext(Context)})";
    }

    private static string FormatCaption(string? caption) => FormatOptional("caption", caption);

    private static string FormatContext(string? context) => FormatOptional("context", context);

    private static string FormatOptional(string label, string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : $", {label}: '{value}'";
}
