using Sigma.Core.Domain.Model.Dto.OpenAPI;
using Sigma.Services.LLamaSharp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Sigma.Controllers
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class LLamaSharpController(ILLamaSharpService _lLamaSharpService, ILogger<LLamaSharpController> _logger) : ControllerBase
    {
        /// <summary>
        /// 本地会话接口
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("llama/v1/chat/completions")]
        public async Task chat(OpenAIModel model)
        {
            _logger.LogInformation("开始：llama/v1/chat/completions");
            if (model.stream)
            {
                await _lLamaSharpService.ChatStream(model, HttpContext);
            }
            else
            {
                await _lLamaSharpService.Chat(model, HttpContext);
            }
            _logger.LogInformation("结束：llama/v1/chat/completions");
        }

        /// <summary>
        /// 本地嵌入接口
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("llama/v1/embeddings")]
        public async Task embedding(OpenAIEmbeddingModel model)
        {
            _logger.LogInformation("开始：llama/v1/embeddings");
            await _lLamaSharpService.Embedding(model, HttpContext);
            _logger.LogInformation("结束：llama/v1/embeddings");

        }
    }
}
