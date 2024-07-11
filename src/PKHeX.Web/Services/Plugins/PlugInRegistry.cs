using System.Reflection;
using PKHeX.Web.Plugins;

namespace PKHeX.Web.Services.Plugins;

public class PlugInRegistry
{
    private readonly IServiceProvider _appServiceProvider;
    private readonly HttpClient _httpClientFactory;
    private readonly Dictionary<string, LoadedPlugIn> _loadedPlugins = new();

    private ServiceProvider _pluginServiceProvider = null!;
    
    public event Action<LoadedPlugIn, ChangeType>? OnPlugInChanged;

    public enum ChangeType
    {
        Registered,
        Deregistered,
    }


    public PlugInRegistry(
        HttpClient httpClientFactory,
        IServiceProvider appServiceProvider)
    {
        _appServiceProvider = appServiceProvider;
        _httpClientFactory = httpClientFactory;

        RefreshServiceProvider();
    }

    public async Task<LoadedPlugIn> RegisterFrom(string url)
    {
        var assembly = await _httpClientFactory.GetByteArrayAsync(url);
        return LoadPlugInFrom(url, assembly);
    }

    public void Register(LoadedPlugIn plugIn)
    {
        _loadedPlugins[plugIn.Id] = plugIn;
        RefreshServiceProvider(_pluginServiceProvider);
        OnPlugInChanged?.Invoke(plugIn, ChangeType.Registered);
    }

    public void Deregister(LoadedPlugIn plugIn)
    {
        _loadedPlugins.Remove(plugIn.Id);
        RefreshServiceProvider(_pluginServiceProvider);
        OnPlugInChanged?.Invoke(plugIn, ChangeType.Deregistered);
    }

    public LoadedPlugIn GetBy(string id) => _loadedPlugins[id];
    
    public IEnumerable<LoadedPlugIn> GetAllPlugins() => _loadedPlugins.Values;

    public LoadedPlugIn GetPlugInOwningHook<T>(T hook) where T : IPluginHook =>
        _loadedPlugins.Values.Single(p => p.Hooks.Any(t => t == hook.GetType()));
    
    public IEnumerable<IPluginHook> GetAllHooksOf(LoadedPlugIn plugin)
    {
        return plugin.Hooks.Select(t => _pluginServiceProvider.GetFromImplementation(t) as IPluginHook)!;
    }
    public IEnumerable<T> GetAllHooks<T>() where T : IPluginHook => _pluginServiceProvider.GetServices<T>();
    public IEnumerable<T> GetAllEnabledHooks<T>() where T : IPluginHook => _pluginServiceProvider.GetServices<T>()
        .Where(h => GetPlugInOwningHook(h).IsPlugInAndHookEnabled(h));

    private LoadedPlugIn LoadPlugInFrom(string url, byte[] assemblyBytes)
    {
        if (assemblyBytes.Length == 0) throw new InvalidOperationException("No plugin found in this assembly");

        var assembly = Assembly.Load(assemblyBytes);
        var loadedPlugIn = LoadedPlugIn.From(url, assembly, assemblyBytes);
        
        Register(loadedPlugIn);

        return loadedPlugIn;
    }

    private void RefreshServiceProvider(ServiceProvider? existing = null)
    {
        existing?.Dispose();
        _pluginServiceProvider = BuildServiceCollection().BuildServiceProvider();
    }

    private IServiceCollection BuildServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IGameProvider>(
            new GameServiceProxy(_appServiceProvider.GetRequiredService<GameService>()));

        return services
            .RegisterPluginAt(_loadedPlugins.Values);
    }

    public static readonly IEnumerable<TrustedPlugins> TrustedSources = [
        new TrustedPlugins("PKHeX.Web.Plugins.AutoLegality", "Auto Legality Mode", "/plugins/PKHeX.Web.Plugins.AutoLegality.dll"),
    #if DEBUG
        new TrustedPlugins("PKHeX.Web.Plugins.Demo", "My Demo Plugin", "/plugins/PKHeX.Web.Plugins.Demo.dll"),
    #endif
    ];
    public record TrustedPlugins(String Id, String Name, String SourceUrl);
}