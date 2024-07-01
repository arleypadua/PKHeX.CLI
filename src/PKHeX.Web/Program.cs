using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PKHeX.Core.Saves.Encryption.Providers;
using PKHeX.Web;
using PKHeX.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<GameService>();
builder.Services.AddSingleton<JsService>();
builder.Services.AddSingleton<AntdThemeService>();
builder.Services.AddSingleton<BlazorAesProvider>();
builder.Services.AddSingleton<BlazorMd5Provider>();

builder.Services.AddAntDesign();

var app = builder.Build();

RuntimeCryptographyProvider.Change(app.Services.GetRequiredService<BlazorAesProvider>());
RuntimeCryptographyProvider.Change(app.Services.GetRequiredService<BlazorMd5Provider>());

await app.RunAsync();