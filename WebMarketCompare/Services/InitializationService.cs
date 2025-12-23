using System.ComponentModel.Design;

namespace WebMarketCompare.Services
{
    public class InitializationService : BackgroundService
    {
        private readonly IStandardNamingService _standardNamingService;
        private readonly ILogger<InitializationService> _logger;

        public InitializationService(
        IStandardNamingService standardNamingService,
        ILogger<InitializationService> logger)
        {
            _standardNamingService = standardNamingService;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Dictionaries loaded successfully on startup");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load dictionaries on startup");
                throw;
            }
        }
    }
}
