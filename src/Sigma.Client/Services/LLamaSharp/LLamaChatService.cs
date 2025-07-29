﻿using Sigma.Core.Options;
using LLama;
using LLama.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sigma.Services.LLamaSharp
{
    public interface ILLamaChatService
    {
        Task<string> ChatAsync(string input);
        IAsyncEnumerable<string> ChatStreamAsync(string input);
    }

    public class LLamaChatService : IDisposable, ILLamaChatService
    {
        private readonly ChatSession _session;
        private readonly LLamaContext _context;
        private readonly ILogger<LLamaChatService> _logger;
        private readonly LLamaSharpOption _option;
        private bool _continue = false;

        private const string SystemPrompt = "You are a personal assistant who needs to help users .";

        public LLamaChatService(ILogger<LLamaChatService> logger, IOptions<LLamaSharpOption> option)
        {
            _option = option.Value;
            var @params = new ModelParams(_option.Chat)
            {
                ContextSize = 2048,
            };

            // todo: share weights from a central service
            using var weights = LLamaWeights.LoadFromFile(@params);

            _logger = logger;
            _context = new LLamaContext(weights, @params);

            _session = new ChatSession(new InteractiveExecutor(_context));
            _session.History.AddMessage(AuthorRole.System, SystemPrompt);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        public async Task<string> ChatAsync(string input)
        {

            if (!_continue)
            {
                _logger.LogInformation("Prompt: {text}", SystemPrompt);
                _continue = true;
            }
            _logger.LogInformation("Input: {text}", input);
            var outputs = _session.ChatAsync(
                new ChatHistory.Message(AuthorRole.User, input),
                new InferenceParams()
                {
                    RepeatPenalty = 1.0f,
                    AntiPrompts = new string[] { "User:" },
                });

            var result = "";
            await foreach (var output in outputs)
            {
                _logger.LogInformation("Message: {output}", output);
                result += output;
            }

            return result;
        }

        public async IAsyncEnumerable<string> ChatStreamAsync(string input)
        {
            if (!_continue)
            {
                _logger.LogInformation(SystemPrompt);
                _continue = true;
            }

            _logger.LogInformation(input);

            var outputs = _session.ChatAsync(
                new ChatHistory.Message(AuthorRole.User, input!)
                , new InferenceParams()
                {
                    RepeatPenalty = 1.0f,
                    AntiPrompts = new string[] { "User:" },
                });

            await foreach (var output in outputs)
            {
                _logger.LogInformation(output);
                yield return output;
            }
        }

    }
}
