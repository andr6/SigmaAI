using System.Threading.Tasks;

namespace Sigma.Core.Domain.Interface
{
    public interface ISecurityAssessmentService
    {
        Task<string> RunDiscoveryAsync(string target);
        Task<string> RunValidationAsync(string data);
        string GetNextAction(string state);
        void UpdatePolicy(string state, string action, double reward);
    }
}
