﻿using Sigma.Core.Domain.Interface;
using Sigma.Core.Domain.Model.Dto.OpenAPI;
using Sigma.Core.Domain.Model.Enum;
using Sigma.Core.Repositories;
using Sigma.Core.Utils;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Sigma.Services.OpenApi
{
    public interface IOpenApiService
    {
        Task Chat(OpenAIModel model, string sk, HttpContext HttpContext);
    }

    public class OpenApiService(
        IApps_Repositories _apps_Repositories,
        IKernelService _kernelService,
        IKMService _kMService,
        IChatService _chatService
    ) : IOpenApiService
    {
        public async Task Chat(OpenAIModel model, string sk, HttpContext HttpContext)
        {
            string headerValue = sk;
            Regex regex = new Regex(@"Bearer (.*)");
            Match match = regex.Match(headerValue);
            string token = match.Groups[1].Value;
            Apps app = _apps_Repositories.GetFirst(p => p.SecretKey == token);
            if (app.IsNotNull())
            {
                (string questions, ChatHistory history) = await GetHistory(model);
                switch (app.Type)
                {
                    case AppType.Chat:
                        // normal conversation
                        if (model.stream)
                        {
                            OpenAIStreamResult result1 = new OpenAIStreamResult();
                            result1.created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                            result1.choices = new List<StreamChoicesModel>()
                                { new StreamChoicesModel() { delta = new OpenAIMessage() { role = "assistant" } } };
                            await SendChatStream(HttpContext, result1, app, questions, history);
                            HttpContext.Response.ContentType = "application/json";
                            await HttpContext.Response.WriteAsync(JsonConvert.SerializeObject(result1));
                            await HttpContext.Response.CompleteAsync();
                            return;
                        }
                        else
                        {
                            OpenAIResult result2 = new OpenAIResult();
                            result2.created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                            result2.choices = new List<ChoicesModel>()
                                { new ChoicesModel() { message = new OpenAIMessage() { role = "assistant" } } };
                            result2.choices[0].message.content = await SendChat(questions, history, app);
                            HttpContext.Response.ContentType = "application/json";
                            await HttpContext.Response.WriteAsync(JsonConvert.SerializeObject(result2));
                            await HttpContext.Response.CompleteAsync();
                        }

                        break;

                    case AppType.Kms:
                        // knowledge base Q&A
                        if (model.stream)
                        {
                            OpenAIStreamResult result3 = new OpenAIStreamResult();
                            result3.created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                            result3.choices = new List<StreamChoicesModel>()
                                { new StreamChoicesModel() { delta = new OpenAIMessage() { role = "assistant" } } };
                            await SendKmsStream(HttpContext, result3, app, questions, history);
                            HttpContext.Response.ContentType = "application/json";
                            await HttpContext.Response.WriteAsync(JsonConvert.SerializeObject(result3));
                            await HttpContext.Response.CompleteAsync();
                        }
                        else
                        {
                            OpenAIResult result4 = new OpenAIResult();
                            result4.created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                            result4.choices = new List<ChoicesModel>()
                                { new ChoicesModel() { message = new OpenAIMessage() { role = "assistant" } } };
                            result4.choices[0].message.content = await SendKms(questions, history, app);
                            HttpContext.Response.ContentType = "application/json";
                            await HttpContext.Response.WriteAsync(JsonConvert.SerializeObject(result4));
                            await HttpContext.Response.CompleteAsync();
                        }

                        break;
                }
            }
        }

        private async Task SendChatStream(HttpContext HttpContext, OpenAIStreamResult result, Apps app, string questions, ChatHistory history)
        {
            HttpContext.Response.Headers.Append("Content-Type", "text/event-stream");
            var chatResult = _chatService.SendChatByAppAsync(app, questions, history);
            await foreach (var content in chatResult)
            {
                result.choices[0].delta.content = content.ConvertToString();
                string message = $"data: {JsonConvert.SerializeObject(result)}\n\n";
                await HttpContext.Response.WriteAsync(message, Encoding.UTF8);
                await HttpContext.Response.Body.FlushAsync();
                // simulate delay
                await Task.Delay(TimeSpan.FromMilliseconds(50));
            }

            await HttpContext.Response.WriteAsync("data: [DONE]");
            await HttpContext.Response.Body.FlushAsync();

            await HttpContext.Response.CompleteAsync();
        }

        /// <summary>
        /// Send a normal conversation
        /// </summary>
        /// <param name="questions"></param>
        /// <param name="history"></param>
        /// <param name="app"></param>
        /// <returns></returns>
        private async Task<string> SendChat(string questions, ChatHistory history, Apps app)
        {
            string result = "";

            if (string.IsNullOrEmpty(app.Prompt) || !app.Prompt.Contains("{{$input}}"))
            {
                // if the template is empty, add a default prompt
                app.Prompt = app.Prompt.ConvertToString() + "{{$input}}";
            }
            KernelArguments args = new KernelArguments();
            if (history.Count > 10)
            {
                app.Prompt = @"${{ConversationSummaryPlugin.SummarizeConversation $history}}" + app.Prompt;
                args = new() {
                { "history", string.Join("\n", history.Select(x => x.Role + ": " + x.Content)) },
                { "input", questions }
                };
            }
            else
            {
                args = new()
                {
                { "input", $"{string.Join("\n", history.Select(x => x.Role + ": " + x.Content))}{Environment.NewLine} user:{questions}" }
                };
            }

            var _kernel = _kernelService.GetKernelByApp(app);
            var temperature = app.Temperature / 100; // value stored as 0~100, scale down
            OpenAIPromptExecutionSettings settings = new() { Temperature = temperature };
            if (!string.IsNullOrEmpty(app.PluginList) || !string.IsNullOrEmpty(app.NativeFunctionList)) // also include local plugins here
            {
                _kernelService.ImportFunctionsByApp(app, _kernel);
                settings.ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions;
            }
            var func = _kernel.CreateFunctionFromPrompt(app.Prompt, settings);
            var chatResult = await _kernel.InvokeAsync(function: func, arguments: args);
            if (chatResult.IsNotNull())
            {
                string answers = chatResult.GetValue<string>();
                result = answers;
            }

            return result;
        }

        private async Task SendKmsStream(HttpContext HttpContext, OpenAIStreamResult result, Apps app, string questions, ChatHistory history)
        {
            HttpContext.Response.Headers.Append("Content-Type", "text/event-stream");
            var chatResult = _chatService.SendKmsByAppAsync(app, questions, history);
            int i = 0;
            await foreach (var content in chatResult)
            {
                result.choices[0].delta.content = content.ConvertToString();
                string message = $"data: {JsonConvert.SerializeObject(result)}\n\n";
                await HttpContext.Response.WriteAsync(message, Encoding.UTF8);
                await HttpContext.Response.Body.FlushAsync();
                // simulate delay
                await Task.Delay(TimeSpan.FromMilliseconds(50));
            }

            await HttpContext.Response.WriteAsync("data: [DONE]");
            await HttpContext.Response.Body.FlushAsync();

            await HttpContext.Response.CompleteAsync();
        }

        /// <summary>
        /// Send knowledge base question answering
        /// </summary>
        /// <param name="questions"></param>
        /// <param name="app"></param>
        /// <returns></returns>
        private async Task<string> SendKms(string questions, ChatHistory history, Apps app)
        {
            string result = "";
            var _kernel = _kernelService.GetKernelByApp(app);

            var relevantSource = await _kMService.GetRelevantSourceList(app.KmsIdList, questions);
            var dataMsg = new StringBuilder();
            if (relevantSource.Any())
            {
                foreach (var item in relevantSource)
                {
                    dataMsg.AppendLine(item.ToString());
                }

                KernelFunction jsonFun = _kernel.Plugins.GetFunction("KMSPlugin", "Ask1");
                var chatResult = await _kernel.InvokeAsync(function: jsonFun,
                    arguments: new KernelArguments() { ["doc"] = dataMsg, ["history"] = string.Join("\n", history.Select(x => x.Role + ": " + x.Content)), ["questions"] = questions });
                if (chatResult.IsNotNull())
                {
                    string answers = chatResult.GetValue<string>();
                    result = answers;
                }
            }

            return result;
        }

        /// <summary>
        /// Conversation summary from history
        /// </summary>
        /// <param name="app"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        private async Task<(string, ChatHistory)> GetHistory(OpenAIModel model)
        {
            ChatHistory history = new ChatHistory();
            string questions = model.messages[model.messages.Count - 1].content;
            for (int i = 0; i < model.messages.Count() - 1; i++)
            {
                var item = model.messages[i];
                if (item.role.ToLower() == "user")
                {
                    history.AddUserMessage(item.content);
                }
                else if (item.role.ToLower() == "assistant")
                {
                    history.AddAssistantMessage(item.content);
                }
                else if (item.role.ToLower() == "system")
                {
                    history.AddSystemMessage(item.content);
                }
            }
            return (questions, history);
        }
    }

}