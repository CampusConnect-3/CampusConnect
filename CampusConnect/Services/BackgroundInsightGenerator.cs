using CampusConnect.Services;

namespace CampusConnect.BackgroundServices;

public class BackgroundInsightGenerator : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundInsightGenerator> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(6); // Run every 6 hours

    public BackgroundInsightGenerator(
        IServiceProvider serviceProvider,
        ILogger<BackgroundInsightGenerator> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background Insight Generator started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, stoppingToken);

                _logger.LogInformation("Starting scheduled insight generation...");

                using var scope = _serviceProvider.CreateScope();
                var aiService = scope.ServiceProvider.GetRequiredService<AIInsightService>();
                
                await aiService.GenerateBuildingInsightsAsync();

                _logger.LogInformation("Scheduled insight generation completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background insight generation");
            }
        }
    }
}