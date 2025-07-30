using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Sigma.Core.Domain.Service
{
    public class ThreatSentinelAgent
    {
        public record InvestigationResult(string Indicator, double RiskScore, string[] Actions);

        public Task<string> InvestigateAsync(string indicator)
        {
            if (string.IsNullOrWhiteSpace(indicator))
            {
                throw new ArgumentException("Indicator cannot be empty", nameof(indicator));
            }

            // Simple heuristic for demonstration
            double score = indicator.StartsWith("10.") ? 0.3 : 0.8;
            string[] actions = score > 0.5
                ? new[] { "block_ip", "notify_team" }
                : new[] { "monitor" };

            var result = new InvestigationResult(indicator, score, actions);
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            };
            return Task.FromResult(JsonConvert.SerializeObject(result, settings));
        }
    }
}
