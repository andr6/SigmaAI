using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Sigma;
using Sigma.Core.Common;

namespace Sigma.plugins.Functions;

public class CyberRangeFunctions
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CyberRangeFunctions(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private void EnsureAdmin()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null || !user.IsInRole(RoleConstants.Admin))
        {
            throw new UnauthorizedAccessException("User is not authorized to invoke this function.");
        }
    }

    /// <summary>
    /// Start the GAN cyber range simulation.
    /// </summary>
    /// <returns>Output from the simulator</returns>
    [SigmaFunction]
    public string StartSimulation()
    {
        EnsureAdmin();
        return RunCommand("start");
    }

    /// <summary>
    /// Get metrics from the running simulation.
    /// </summary>
    /// <returns>JSON string of metrics</returns>
    [SigmaFunction]
    public string GetMetrics()
    {
        EnsureAdmin();
        return RunCommand("metrics");
    }

    /// <summary>
    /// Stop the running GAN cyber range simulation.
    /// </summary>
    /// <returns>Output from the simulator</returns>
    [SigmaFunction]
    public string StopSimulation()
    {
        EnsureAdmin();
        return RunCommand("stop");
    }

    private static string RunCommand(string command)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "python3",
            Arguments = $"scripts/gan_range_runner.py {command}",
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
}
