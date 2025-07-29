using Microsoft.Extensions.Logging;
using Sigma.Core.Domain.Interface;

namespace Sigma.Core.Domain.Service
{
    public class ModelMetricsService : IModelMetricsService
    {
        private readonly ILogger<ModelMetricsService> _logger;
        public ModelMetricsService(ILogger<ModelMetricsService> logger)
        {
            _logger = logger;
        }

        public Task LogUsageAsync(string modelName, TimeSpan duration, bool success)
        {
            _logger.LogInformation("Model {model} used - success:{success} duration:{duration}", modelName, success, duration);
            return Task.CompletedTask;
        }
    }
}
