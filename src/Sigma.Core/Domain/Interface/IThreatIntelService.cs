using System.Threading.Tasks;
using Sigma.Core.Repositories;

namespace Sigma.Core.Domain.Interface
{
    public interface IThreatIntelService
    {
        Task<string> RetrieveThreatDataAsync(string query);
        Task<string> GenerateReportAsync(Apps app, string threatData);
    }
}
