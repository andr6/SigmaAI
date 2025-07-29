using Sigma.Core.Options;
using LLama;
using LLama.Common;
using Microsoft.Extensions.Options;

namespace Sigma.Services.LLamaSharp
{
    public interface ILLamaEmbeddingService
    {
        Task<List<float>> Embedding(string text);
    }

    /// <summary>
    /// 本地Embedding
    /// </summary>
    public class LLamaEmbeddingService : IDisposable, ILLamaEmbeddingService
    {
        private LLamaEmbedder _embedder;
        private readonly LLamaSharpOption _option;

        public LLamaEmbeddingService(IOptions<LLamaSharpOption> option)
        {
            _option = option.Value;

            var @params = new ModelParams(_option.Embedding) { EmbeddingMode = true };
            using var weights = LLamaWeights.LoadFromFile(@params);
            _embedder = new LLamaEmbedder(weights, @params);
        }
        public void Dispose()
        {
            _embedder?.Dispose();
        }

        public async Task<List<float>> Embedding(string text)
        {
            float[] embeddings = await _embedder.GetEmbeddings(text);
            //PG只有1536维
            return embeddings.ToList();
        }
    }
}
