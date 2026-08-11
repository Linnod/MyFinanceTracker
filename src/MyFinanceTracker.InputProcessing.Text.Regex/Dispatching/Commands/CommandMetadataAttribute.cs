namespace MyFinanceTracker.InputProcessing.Text.Regex.Dispatching.Commands;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
internal sealed class CommandMetadataAttribute(string description, params string[] examples) : Attribute
{
    public string Description { get; } = description;
    public string[] Examples { get; } = examples;
}