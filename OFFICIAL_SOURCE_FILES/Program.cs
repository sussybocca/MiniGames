using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MiniGames.Services;
using MiniGames;
using MiniGames.CustomServices.Services;
using MiniGames.AI_OFFLINE_SERVICES;
using Toolbelt.Blazor.Extensions.DependencyInjection;

// Direct references to BrokenForge files
using GameState = MiniGames.BlazorGames.BrokenForge.Models.GameState;
using GameService = MiniGames.BlazorGames.BrokenForge.Services.GameService;
using WorldGenerator = MiniGames.BlazorGames.BrokenForge.Services.WorldGenerator;
using TileType = MiniGames.BlazorGames.BrokenForge.Models.TileType;
using Player = MiniGames.BlazorGames.BrokenForge.Models.Player;
using Enemy = MiniGames.BlazorGames.BrokenForge.Models.Enemy;
using Item = MiniGames.BlazorGames.BrokenForge.Models.Item;
using Weapon = MiniGames.BlazorGames.BrokenForge.Models.Weapon;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add root App component
builder.RootComponents.Add<App>("#app");

// Only add BrokenForgeApp if the element exists
var brokenForgeElement = builder.Configuration.GetValue<string>("BrokenForgeElement") ?? "#brokenforge-app";
try
{
    builder.RootComponents.Add<MiniGames.BlazorGames.BrokenForge.BrokenForgeApp>(brokenForgeElement);
}
catch
{
    // Element doesn't exist yet - will be added after build
}

builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<MiniGames.Services.GameService>();
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

// Gamepad support
builder.Services.AddGamepadList();

// ---------- BrokenForge Registrations ----------
builder.Services.AddSingleton<MiniGames.BlazorGames.BrokenForge.Models.GameState>();
builder.Services.AddScoped<MiniGames.BlazorGames.BrokenForge.Services.GameService>(sp =>
{
    var state = sp.GetRequiredService<MiniGames.BlazorGames.BrokenForge.Models.GameState>();
    var seed = new Random().Next();
    return new MiniGames.BlazorGames.BrokenForge.Services.GameService(state, seed);
});
// -----------------------------------------------

var host = builder.Build();

// ---------- Generate the world after host is built ----------
var gameState = host.Services.GetRequiredService<MiniGames.BlazorGames.BrokenForge.Models.GameState>();
var worldGenerator = new MiniGames.BlazorGames.BrokenForge.Services.WorldGenerator(
    gameState.WorldWidth, 
    gameState.WorldHeight, 
    new Random().Next()
);
gameState.World = worldGenerator.Generate();
// ------------------------------------------------------------

await host.RunAsync();