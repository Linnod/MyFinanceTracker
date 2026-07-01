namespace MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
internal sealed class CommandMetadataAttribute(string description, string? usageHint = null, params string[] examples) : Attribute
{
    public string Description { get; } = description;
    public string? UsageHint { get; } = usageHint;
    public string[] Examples { get; } = examples;
}