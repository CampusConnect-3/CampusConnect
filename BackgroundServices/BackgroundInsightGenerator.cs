using CampusConnect.Services;
using MongoDB.Driver;

namespace CampusConnect.BackgroundServices;

public class BackgroundInsightGenerator : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundInsightGenerator> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

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

        // Wait a bit before starting
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GenerateInsightsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background insight generation");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Background Insight Generator stopped");
    }

    private async Task GenerateInsightsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var mongoService = scope.ServiceProvider.GetRequiredService<MongoDBService>();
        var geminiService = scope.ServiceProvider.GetRequiredService<GeminiInsightService>();

        if (mongoService.RequestSnapshots == null || mongoService.AIInsights == null)
        {
            _logger.LogWarning("MongoDB unavailable for insight generation");
            return;
        }

        try
        {
            // Find requests without insights
            var allSnapshots = await mongoService.RequestSnapshots
                .Find(_ => true)
                .ToListAsync(cancellationToken);

            var existingInsightIds = await mongoService.AIInsights
                .Find(_ => true)
                .Project(i => i.RequestId)
                .ToListAsync(cancellationToken);

            var requestsNeedingInsights = allSnapshots
                .Where(s => !existingInsightIds.Contains(s.RequestId))
                .Take(5) // Process 5 at a time
                .ToList();

            if (!requestsNeedingInsights.Any())
            {
                _logger.LogDebug("No requests need insights at this time");
                return;
            }

            _logger.LogInformation("Generating insights for {Count} requests", requestsNeedingInsights.Count);

            foreach (var snapshot in requestsNeedingInsights)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    var insight = await geminiService.GenerateInsightAsync(snapshot);

                    if (insight != null)
                    {
                        await mongoService.AIInsights.InsertOneAsync(insight, cancellationToken: cancellationToken);
                        _logger.LogInformation("Generated AI insight for request {RequestId}", snapshot.RequestId);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to generate insight for request {RequestId}", snapshot.RequestId);
                    }

                    // Rate limiting - wait between API calls
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating insight for request {RequestId}", snapshot.RequestId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GenerateInsightsAsync");
        }
    }
}