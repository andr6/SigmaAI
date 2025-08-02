using Microsoft.AspNetCore.Mvc;
using Sigma.Core.Domain.Service;
using System.Diagnostics;

namespace Sigma.Controllers
{
    /// <summary>
    /// APIs for scheduling and retrieving simulation runs.
    /// </summary>
    [ApiController]
    [Route("api/simulations")]
    public class SimulationController : ControllerBase
    {
        private readonly BackgroundJobService _backgroundJobService;
        private readonly SimulationService _simulationService;

        public SimulationController(BackgroundJobService backgroundJobService, SimulationService simulationService)
        {
            _backgroundJobService = backgroundJobService;
            _simulationService = simulationService;
        }

        /// <summary>
        /// Schedule an Atomic Red Team simulation.
        /// </summary>
        [HttpPost("schedule")]
        public IActionResult Schedule([FromBody] SimulationRequest request)
        {
            _backgroundJobService.Enqueue(async () =>
            {
                var result = RunAtomicTest(request.TestId);
                _simulationService.AddResult(result);
            });
            return Accepted();
        }

        /// <summary>
        /// Retrieve captured simulation results.
        /// </summary>
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            return Ok(_simulationService.GetResults());
        }

        private static string RunAtomicTest(string testId)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "bash",
                Arguments = $"-lc \"echo Running atomic test {testId}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            using var process = Process.Start(psi)!;
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            return process.ExitCode == 0 ? output.Trim() : $"Error: {error}";
        }

        public record SimulationRequest(string TestId);
    }
}
