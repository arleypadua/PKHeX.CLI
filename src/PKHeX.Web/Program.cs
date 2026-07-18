using System.Text.Json;
using System.Text.Json.Serialization;
using Blazor.Analytics;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PKHeX.Core;
using PKHeX.Facade.Repositories;
using PKHeX.Web;
using PKHeX.Web.BackendApi;
using PKHeX.Web.BackendApi.Repositories;
using PKHeX.Web.Extensions;
using PKHeX.Web.Services;
using PKHeX.Web.Services.AnalyticsResults;
using PKHeX.Web.Services.Auth;
using PKHeX.Web.Services.GeneralSettings;
using PKHeX.Web.Services.Plugins;
using Sentry.Extensions.Logging;
using Sentry.Protocol;
using TG.Blazor.IndexedDB;
using App = PKHeX.Web.App;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.HostEnvironment.Environment}.json", optional: true, reloadOnChange: true);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddHttpClient("BackendApi", client =>
    {
        var configuration = builder.Configuration.GetBackendApiOptions();
        if (!string.IsNullOrWhiteSpace(configuration.BaseUri))
        {
            client.BaseAddress = new Uri(configuration.BaseUri);
        }
    })
    .AddHttpMessageHandler<BackendApiAuthHandler>();

builder.Services.AddHttpClient("BackendApi.Anonymous", client =>
{
    var configuration = builder.Configuration.GetBackendApiOptions();
    if (!string.IsNullOrWhiteSpace(configuration.BaseUri))
    {
        client.BaseAddress = new Uri(configuration.BaseUri);
    }
});

builder.Services.AddScoped<BackendApiAuthHandler>();

builder.Services.AddScoped<GameService>();
builder.Services.AddScoped<EncounterService>();
builder.Services.AddScoped<LoadPokemonService>();
builder.Services.AddScoped<SmogonMoveSetRepository>();
builder.Services.AddScoped<AnalyticsResultsService>();
builder.Services.AddScoped<NewsService>();

builder.Services.AddScoped<PlugInService>();
builder.Services.AddScoped<PlugInRegistry>();
builder.Services.AddScoped<PlugInRuntime>();
builder.Services.AddScoped<PlugInRegistry>();
builder.Services.AddScoped<PlugInLocalStorage>();
builder.Services.AddScoped<PlugInLocalStorageLoader>();
builder.Services.AddScoped<PlugInSourceService>();
builder.Services.AddScoped<PlugInSourceLocalStorage>();
builder.Services.AddScoped<PlugInPageRegistry>();
builder.Services.AddScoped<PlugInFilesRepository>();

builder.Services.AddScoped<UserJourneyService>();
builder.Services.AddScoped<GeneralSettingsService>();
builder.Services.AddScoped<AnalyticsService>();
builder.Services.AddScoped<JsService>();
builder.Services.AddScoped<AntdThemeService>();
builder.Services.AddScoped<ClipboardService>();
builder.Services.AddScoped<BrowserWindowService.Instance>();
builder.Services.AddScoped<AuthService>();

builder.Services.AddScoped<BlazorAesProvider>();
builder.Services.AddScoped<BlazorMd5Provider>();

builder.Services.AddScoped<SyncPokemonQueue>();
builder.Services.AddScoped<MyPokemonRepository>();
builder.Services.AddScoped<PublicPokemonRepository>();
builder.Services.AddScoped<LocalSyncedPokemonRepository>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<SyncPokemonWorker>();

builder.Services.AddAntDesign();
builder.Services.AddGoogleAnalytics("G-BV586KEZM9");

builder.Services.AddBlazoredLocalStorage(config =>
{
    config.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    config.JsonSerializerOptions.IgnoreReadOnlyProperties = true;
    config.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    config.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    config.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
    config.JsonSerializerOptions.WriteIndented = false;
});

builder.Services.AddIndexedDB(store =>
{
    store.DbName = "pkhex-web-db";
    store.Version = 1;

    store.Stores.Add(PlugInFilesRepository.Schema);
});

builder.Services.ConfigureCookieConsent();

#if !DEBUG
builder.UseSentry(options =>
{
    options.Dsn = "https://48a86c94313f2f1c2066dee9be6add57@o4507742210949120.ingest.de.sentry.io/4507742217175120";
    options.TracesSampleRate = 0.1;

    options.SetBeforeSend((e, _) =>
    {
        // it has been observed that very often ant is not yet ready and throws a lot of exceptions during the render time
        // but eventually the UI just works
        // so we simply skip these issues
        var exception = e.Exception;
        var innerException = e.Exception?.InnerException;
        var data = exception?.Data ?? new Dictionary<string, object>();
        if (innerException is not null)
        {
            var mechanism = innerException.Data[Mechanism.MechanismKey];
            if (mechanism is not null) data.Add(Mechanism.MechanismKey, mechanism);
        }
        
        var skipException = e.Exception is not null
                            && data[Mechanism.MechanismKey]?.Equals("UnobservedTaskException") == true
                            && (
                                e.Exception.StackTrace?.Contains("ant-design-blazor.js") == true
                                || e.Exception.ToString().Contains("ant-design-blazor.js")
                            );

        return skipException
            ? null
            : e;
    });
});

builder.Logging.AddSentry(o => o.InitializeSdk = false);
#endif

var app = builder.Build();

// Although Blazor WASM can target the whole .NET Framework API surface,
// during the Runtime, Microsoft has disabled the native support to some APIs under the System.Security.Cryptography namespace
// During startup we replace PKHeX unsupported cryptography APIs with a javascript-based alternative 
RuntimeCryptographyProvider.Aes = app.Services.GetRequiredService<BlazorAesProvider>();
RuntimeCryptographyProvider.Md5 = app.Services.GetRequiredService<BlazorMd5Provider>();

await app.ConfigureCookieConsentToggle();

#if DEBUG
app.Services.GetRequiredService<IAnalytics>()
    .Disable();
#endif

await app.Services.GetRequiredService<PlugInLocalStorageLoader>()
    .InitializePlugIns();

app.Services.GetRequiredService<SyncPokemonWorker>().Start();

await app.RunAsync();
