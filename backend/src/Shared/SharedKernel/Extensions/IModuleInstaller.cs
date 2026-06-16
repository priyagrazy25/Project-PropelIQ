using Microsoft.Extensions.DependencyInjection;

namespace SharedKernel.Extensions;

public interface IModuleInstaller
{
    void Install(IServiceCollection services);
}
