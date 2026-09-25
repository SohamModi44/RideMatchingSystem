using Microsoft.Extensions.DependencyInjection;

namespace RideMatchingSystem.Services;

public class RideMatchingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RideMatchingBackgroundService> _logger;

    public RideMatchingBackgroundService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<RideMatchingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Ride matching background service started.");

        var intervalSeconds =
            _configuration.GetValue<int>(
                "RideMatching:MatchingIntervalSeconds");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope =
                    _scopeFactory.CreateScope();

                var matchingService =
                    scope.ServiceProvider
                        .GetRequiredService<RideMatchingService>();

                await matchingService
                    .MatchPendingRidesAsync(
                        stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred during ride matching.");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(intervalSeconds),
                stoppingToken);
        }

        _logger.LogInformation(
            "Ride matching background service stopped.");
    }
}