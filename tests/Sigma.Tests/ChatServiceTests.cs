using Sigma.Core.Domain.Service;
using Sigma.Core.Domain.Interface;
using Sigma.Core.Repositories;
using SigmaChatHistory = Sigma.Core.Domain.Chat.ChatHistory;
using Sigma.Core.Domain.Chat;
using Moq;

namespace Sigma.Tests
{
    public class ChatServiceTests
    {
        [Fact]
        public void GetChatHistory_ReturnsCorrectHistory()
        {
            var kernelService = new Mock<IKernelService>();
            var kmService = new Mock<IKMService>();
            var repo = new Mock<IKmsDetails_Repositories>();
            var metrics = new Mock<IModelMetricsService>();

            var service = new ChatService(kernelService.Object, kmService.Object, repo.Object, metrics.Object);

            var list = new List<SigmaChatHistory>
            {
                new SigmaChatHistory { Role = ChatRoles.User, Content = "Hi" },
                new SigmaChatHistory { Role = ChatRoles.Assistant, Content = "Hello" }
            };

            var result = service.GetChatHistory(list);

            Assert.Equal(2, result.Count);
            Assert.Equal("Hello", result.Last().Content);
        }
    }
}
