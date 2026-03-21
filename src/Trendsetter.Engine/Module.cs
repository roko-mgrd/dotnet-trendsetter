namespace Trendsetter.Engine;

using System.Reflection;
using Trendsetter.Engine.Builders;
using Trendsetter.Engine.Configuration;
using Trendsetter.Engine.Contracts;

public sealed class Module
{
    private readonly Dictionary<Type, ScoringConfiguration> _configs = [];

    /// <summary>
    /// Register a configuration by instance.
    /// registry.Register(new ProcedureConfiguration());
    /// </summary>
    public Module Register<TModel>(ITrendConfiguration<TModel> configuration)
    {
        var builder = new TrendModelBuilder<TModel>();
        configuration.Configure(builder);
        _configs[typeof(TModel)] = builder.Configuration;
        return this;
    }

    /// <summary>
    /// Register a configuration by type (must have parameterless constructor).
    /// registry.Register[ProcedureConfiguration]()
    /// </summary>
    public Module Register<TConfiguration, TModel>()
        where TConfiguration : ITrendConfiguration<TModel>, new()
    {
        return Register(new TConfiguration());
    }

    /// <summary>
    /// Scan an assembly for all ITrendConfiguration implementations and register them.
    /// </summary>
    public Module RegisterFromAssembly(Assembly assembly)
    {
        var configTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ITrendConfiguration<>))
                .Select(i => (ConfigType: t, ModelType: i.GetGenericArguments()[0])));

        foreach (var (configType, modelType) in configTypes)
        {
            var instance = Activator.CreateInstance(configType)!;
            var registerMethod = typeof(Module)
                .GetMethod(nameof(Register), [typeof(ITrendConfiguration<>).MakeGenericType(modelType)])!
                .MakeGenericMethod(modelType); // not needed, it's already generic on TModel
            // Use reflection to call Register<TModel>(ITrendConfiguration<TModel>)
            var registerGeneric = typeof(Module)
                .GetMethods()
                .First(m => m.Name == nameof(Register) &&
                            m.GetParameters().Length == 1 &&
                            m.GetParameters()[0].ParameterType.IsGenericType)
                .MakeGenericMethod(modelType);

            registerGeneric.Invoke(this, [instance]);
        }

        return this;
    }

    public ScoringConfiguration GetConfiguration<TModel>()
    {
        return GetConfiguration(typeof(TModel));
    }

    public ScoringConfiguration GetConfiguration(Type modelType)
    {
        if (_configs.TryGetValue(modelType, out var config))
        {
            return config;
        }

        // Return a default configuration with auto-detection if nothing registered
        return new ScoringConfiguration();
    }

    public bool HasConfiguration<TModel>()
    {
        return _configs.ContainsKey(typeof(TModel));
    }
}
