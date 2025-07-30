using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Sigma.Core.Domain.Interface;
using Sigma.Core.Repositories;
using Sigma.Core.Utils;
using Newtonsoft.Json;
using System.Text;

namespace Sigma.Core.Domain.Service
{
    public class ThreatIntelService : IThreatIntelService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly IKernelService _kernelService;
        private readonly ThreatSentinelAgent _agent = new();

        public ThreatIntelService(HttpClient httpClient, IConfiguration config, IKernelService kernelService)
        {
            _httpClient = httpClient;
            _config = config;
            _kernelService = kernelService;
        }

        public async Task<string> RetrieveThreatDataAsync(string query)
        {
            var endpoint = _config["ThreatIntel:ExaEndpoint"] ?? "https://api.exa.ai/search";
            var apiKey = _config["ThreatIntel:ExaApiKey"] ?? Environment.GetEnvironmentVariable("EXA_API_KEY");
            var url = $"{endpoint}?q={Uri.EscapeDataString(query)}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrEmpty(apiKey))
            {
                request.Headers.Add("Authorization", $"Bearer {apiKey}");
            }
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GenerateReportAsync(Apps app, string threatData)
        {
            var kernel = _kernelService.GetKernelByApp(app);
            await _kernelService.ImportFunctionsByApp(app, kernel);
            var promptPath = Path.Combine(RepoFiles.SamplePluginsPath(), "ThreatIntelPlugin", "GenerateReport", "skprompt.txt");
            var prompt = await File.ReadAllTextAsync(promptPath);
            var func = kernel.CreateFunctionFromPrompt(prompt);
            var result = await kernel.InvokeAsync(func, new() { ["data"] = threatData });
            return result.GetValue<string>();
        }

        public Task<string> InvestigateWithThreatSentinelAsync(string indicator)
        {
            return _agent.InvestigateAsync(indicator);
        }
    }
}
