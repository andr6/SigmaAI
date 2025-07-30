using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Sigma.Core.Domain.Service;
using Xunit;

namespace Sigma.Tests
{
    public class SecurityAssessmentServiceTests
    {
        [Fact]
        public void UpdatePolicy_StoresQValue()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string>()).Build();
            var service = new SecurityAssessmentService(config);
            service.UpdatePolicy("s", "a", 1.0);
            Assert.True(service.QTable.ContainsKey(("s", "a")));
        }
    }
}
