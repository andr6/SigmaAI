using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Sigma;
using Sigma.Core.Common;

namespace Sigma.plugins.Functions;

public class CyberIntelFunctions
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CyberIntelFunctions(IHttpContextAccessor httpContextAccessor)
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
    /// Extract IOCs from a CTI PDF report using the CyberIntel runner.
    /// </summary>
    /// <param name="pdfPath">Path to the PDF report</param>
    /// <returns>JSON string of extracted IOCs</returns>
    [SigmaFunction]
    public string ExtractIocs(string pdfPath)
    {
        EnsureAdmin();
        var psi = new ProcessStartInfo
        {
            FileName = "python3",
            Arguments = $"scripts/cyberintel_runner.py \"{pdfPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var process = Process.Start(psi)!;
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            return $"Error: {error}";
        }
        return output.Trim();
    }

    /// <summary>
    /// Generate detection rules for Sentinel, Splunk and network devices.
    /// </summary>
    /// <param name="iocJson">JSON string produced by ExtractIocs</param>
    /// <returns>JSON string with rules</returns>
    [SigmaFunction]
    public string GenerateRules(string iocJson)
    {
        EnsureAdmin();
        var document = JsonDocument.Parse(iocJson);
        var rules = new Dictionary<string, List<string>>
        {
            ["sentinel"] = new(),
            ["splunk"] = new(),
            ["ids_ips"] = new(),
            ["firewall"] = new()
        };

        if (document.RootElement.TryGetProperty("ips", out var ips))
        {
            foreach (var ip in ips.EnumerateArray().Select(e => e.GetString()))
            {
                if (string.IsNullOrWhiteSpace(ip)) continue;
                rules["sentinel"].Add($"SecurityEvent | where EventData contains '{ip}'");
                rules["splunk"].Add($"search index=* \"{ip}\"");
                rules["ids_ips"].Add($"alert ip any any -> {ip} any (msg:'IOC hit';)");
                rules["firewall"].Add($"block ip {ip}");
            }
        }

        if (document.RootElement.TryGetProperty("domains", out var domains))
        {
            foreach (var domain in domains.EnumerateArray().Select(e => e.GetString()))
            {
                if (string.IsNullOrWhiteSpace(domain)) continue;
                rules["sentinel"].Add($"SecurityEvent | where EventData contains '{domain}'");
                rules["splunk"].Add($"search index=* \"{domain}\"");
                rules["ids_ips"].Add($"alert tcp any any -> any 80 (msg:'IOC domain';content:'{domain}';)");
                rules["firewall"].Add($"block domain {domain}");
            }
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(rules, options);
    }
}
