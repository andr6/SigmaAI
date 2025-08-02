namespace Sigma.Core.Options;

public class PrometheusConfig
{
    public bool Enabled { get; set; } = true;

    public string MetricsEndpoint { get; set; } = "/metrics";
}

