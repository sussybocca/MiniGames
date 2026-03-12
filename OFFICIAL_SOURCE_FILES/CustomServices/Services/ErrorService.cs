using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MiniGames.CustomServices.Services;

public class ErrorService : IErrorService
{
    private readonly HttpClient _http;
    private readonly ILogger<ErrorService> _logger;

    public ErrorService(HttpClient http, ILogger<ErrorService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task SubmitErrorReportAsync(object report)
    {
        try
        {
            // In production, send to a real API endpoint
            var json = JsonSerializer.Serialize(report);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            // var response = await _http.PostAsync("https://api.minigames.example/errors", content);
            // response.EnsureSuccessStatusCode();

            // For now, just log
            _logger.LogInformation("Error report received: {Report}", json);

            // Simulate async work
            await Task.Delay(500);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit error report");
            throw; // Re-throw to let the UI handle it
        }
    }
}