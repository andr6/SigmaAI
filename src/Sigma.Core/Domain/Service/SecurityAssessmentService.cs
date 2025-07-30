using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Sigma.Core.Domain.Interface;

namespace Sigma.Core.Domain.Service
{
    public class SecurityAssessmentService : ISecurityAssessmentService
    {
        private readonly Dictionary<(string state, string action), double> _qTable = new();
        private readonly string _blackboardPath;
        private readonly Random _rand = new();

        public IReadOnlyDictionary<(string state, string action), double> QTable => _qTable;

        public SecurityAssessmentService(IConfiguration config)
        {
            _blackboardPath = config["SecurityAssessment:Blackboard"] ?? "blackboard.jsonl";
        }

        public async Task<string> RunDiscoveryAsync(string target)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "nmap",
                Arguments = $"{target} -oX -",
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            try
            {
                using var proc = Process.Start(psi)!;
                var output = await proc.StandardOutput.ReadToEndAsync();
                await proc.WaitForExitAsync();

                var record = new { Target = target, Output = output, Time = DateTime.UtcNow };
                Directory.CreateDirectory(Path.GetDirectoryName(_blackboardPath) ?? ".");
                await File.AppendAllTextAsync(_blackboardPath, JsonConvert.SerializeObject(record) + Environment.NewLine);
                return output;
            }
            catch (Exception ex)
            {
                return $"error: {ex.Message}";
            }
        }

        public Task<string> RunValidationAsync(string data)
        {
            // Placeholder for validation logic using LLM or other tools
            return Task.FromResult($"Validated: {data}");
        }

        public string GetNextAction(string state)
        {
            var actions = new[] { "discover", "validate" };
            return actions[_rand.Next(actions.Length)];
        }

        public void UpdatePolicy(string state, string action, double reward)
        {
            var key = (state, action);
            if (!_qTable.ContainsKey(key))
            {
                _qTable[key] = 0;
            }
            _qTable[key] = _qTable[key] + 0.1 * (reward - _qTable[key]);
        }
    }
}
