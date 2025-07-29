using System.Threading.Tasks;

namespace Sigma.Core.Domain.Interface
{
    public interface IModelMetricsService
    {
        Task LogUsageAsync(string modelName, TimeSpan duration, bool success);
    }
}
