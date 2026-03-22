namespace Trendsetter.TestAdapter;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Implement in your test assembly to configure DI for trend tests.
/// The test adapter discovers this via reflection and uses it to build
/// the <see cref="IServiceProvider"/> that resolves test instances.
/// </summary>
public interface ITrendTestStartup
{
    void ConfigureServices(IServiceCollection services);
}
