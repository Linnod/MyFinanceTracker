using Microsoft.Extensions.DependencyInjection;

namespace MyFinanceTracker.InputProcessing.Text;

public interface IProcessorConfigured { }

public interface ITextInputProcessingBuilder
{
    IServiceCollection Services { get; }
}

internal sealed class TextInputProcessingBuilder(IServiceCollection services) 
    : ITextInputProcessingBuilder
{
    public IServiceCollection Services { get; } = services;
}

public sealed class ProcessorConfigured(IServiceCollection services) : IProcessorConfigured
{
    public IServiceCollection Services { get; } = services;
}