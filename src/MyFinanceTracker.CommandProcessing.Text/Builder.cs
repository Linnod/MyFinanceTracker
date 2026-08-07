using Microsoft.Extensions.DependencyInjection;

namespace MyFinanceTracker.CommandProcessing.Text;

public interface IProcessorConfigured { }

public interface ITextCommandProcessingBuilder
{
    IServiceCollection Services { get; }
}

internal sealed class TextCommandProcessingBuilder(IServiceCollection services) 
    : ITextCommandProcessingBuilder
{
    public IServiceCollection Services { get; } = services;
}

public sealed class ProcessorConfigured(IServiceCollection services) : IProcessorConfigured
{
    public IServiceCollection Services { get; } = services;
}