using System.Collections.Concurrent;

namespace Sigma.Core.Domain.Service
{
    /// <summary>
    /// Simple in-memory storage for simulation results.
    /// </summary>
    public class SimulationService
    {
        private readonly ConcurrentQueue<string> _results = new();

        public void AddResult(string result)
        {
            _results.Enqueue(result);
        }

        public IEnumerable<string> GetResults()
        {
            return _results.ToArray();
        }
    }
}
