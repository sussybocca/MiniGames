using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MiniGames.Services;
using MiniGames;
using MiniGames.CustomServices.Services;
using MiniGames.AI_OFFLINE_SERVICES; // Added for JSON game generator

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<GameService>();
builder.Services.AddSingleton<ChaosService>();
builder.Services.AddSingleton<RouteChaosService>();


// Custom Services
builder.Services.AddScoped<IEmergencyService, EmergencyService>();
builder.Services.AddScoped<IErrorService, ErrorService>();
builder.Services.AddScoped<IHelpService, HelpService>();
builder.Services.AddScoped<IGuideService, GuideService>();
builder.Services.AddScoped<ILicenseService, LicenseService>();
builder.Services.AddScoped<ILegalService, LegalService>();

// Offline game generator (JSON Service)
builder.Services.AddScoped<OfflineGameGenerator>();

// Trippy services
builder.Services.AddScoped<TrippyCompilerService>();
builder.Services.AddSingleton<TrippyService>();

// TV service
builder.Services.AddScoped<MiniGames.Emulators.TV.TVService>();



await builder.Build().RunAsync();