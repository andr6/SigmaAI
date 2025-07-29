using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Moq;
using Sigma.Core.Domain.Interface;
using Sigma.Core.Domain.Service;
using Sigma.Core.Repositories;
using Xunit;

namespace Sigma.Tests
{
    public class ThreatIntelServiceTests
    {
        private class TestHandler : HttpMessageHandler
        {
            public HttpRequestMessage? LastRequest { get; private set; }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequest = request;
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("test")
                };
                return Task.FromResult(response);
            }
        }

        [Fact]
        public async Task RetrieveThreatDataAsync_SendsRequest()
        {
            var handler = new TestHandler();
            var client = new HttpClient(handler);
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string>
            {
                ["ThreatIntel:ExaEndpoint"] = "https://example.com/search",
                ["ThreatIntel:ExaApiKey"] = "key"
            }).Build();
            var kernelService = new Mock<IKernelService>();
            var service = new ThreatIntelService(client, config, kernelService.Object);

            var result = await service.RetrieveThreatDataAsync("cve-123");

            Assert.Equal("test", result);
            Assert.NotNull(handler.LastRequest);
            Assert.Equal("https://example.com/search?q=cve-123", handler.LastRequest!.RequestUri!.ToString());
            Assert.True(handler.LastRequest!.Headers.Contains("Authorization"));
        }
    }
}
