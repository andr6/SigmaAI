using Microsoft.Extensions.Logging;
using Prometheus;
using Sigma.Core.Domain.Interface;

namespace Sigma.Core.Domain.Service
{
    public class ModelMetricsService : IModelMetricsService
    {
        private static readonly Counter ModelUsageCounter = Metrics.CreateCounter(
            "model_usage_total", "Total model usage", new CounterConfiguration
            {
                LabelNames = ["model", "success"]
            });

        private static readonly Histogram ModelUsageDuration = Metrics.CreateHistogram(
            "model_usage_duration_seconds", "Model usage duration in seconds", new HistogramConfiguration
            {
                LabelNames = ["model", "success"]
            });

        private readonly ILogger<ModelMetricsService> _logger;

        public ModelMetricsService(ILogger<ModelMetricsService> logger)
        {
            _logger = logger;
        }

        public Task LogUsageAsync(string modelName, TimeSpan duration, bool success)
        {
            var successLabel = success ? "true" : "false";
            ModelUsageCounter.WithLabels(modelName, successLabel).Inc();
            ModelUsageDuration.WithLabels(modelName, successLabel).Observe(duration.TotalSeconds);

            _logger.LogInformation("Model {model} used - success:{success} duration:{duration}", modelName, success, duration);
            return Task.CompletedTask;
        }
    }
}
