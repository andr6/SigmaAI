using Sigma.LLM.SparkDesk;
using Sigma.Core.Domain.Interface;
using Sigma.Core.Domain.Other;
using Sigma.Core.Repositories;
using Sigma.Core.Utils;
using LLama;
using LLamaSharp.SemanticKernel.TextCompletion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins.Core;
using Microsoft.SemanticKernel.TextGeneration;
using RestSharp;
using System;
using Sigma.LLM.Mock;
using Sigma.Core.Domain.Model.Enum;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.SemanticKernel.Plugins.OpenApi;
using Sigma.Core.Repositories.AI.Plugin;
using Microsoft.SemanticKernel.ChatCompletion;
using LLamaSharp.SemanticKernel.ChatCompletion;

namespace Sigma.Core.Domain.Service
{
    public class KernelService : IKernelService
    {
        private readonly IPluginRepository _pluginRepository;
        private readonly IAIModels_Repositories _aIModels_Repositories;
        private readonly FunctionService _functionService;
        private readonly IServiceProvider _serviceProvider;
        private Kernel _kernel;

        public KernelService(
              IPluginRepository apis_Repositories,
              IAIModels_Repositories aIModels_Repositories,
              FunctionService functionService,
              IServiceProvider serviceProvider)
        {
            _pluginRepository = apis_Repositories;
            _aIModels_Repositories = aIModels_Repositories;
            _functionService = functionService;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Get a kernel instance. Dependency injection cannot import different plugins per user, so create a new kernel each time.
        /// </summary>
        /// <param name="modelId"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public Kernel GetKernelByApp(Apps app)
        {
            var chatModel = _aIModels_Repositories.GetFirst(p => p.Id == app.ChatModelID);
            app.AIModel = chatModel;

            var builder = Kernel.CreateBuilder();
            WithTextGenerationByAIType(builder, app, chatModel);

            _kernel = builder.Build();
            RegisterPluginsWithKernel(_kernel);
            return _kernel;
        }

        private void WithTextGenerationByAIType(IKernelBuilder builder, Apps app, AIModels chatModel)
        {
            switch (chatModel.AIType)
            {
                case Model.Enum.AIType.OpenAI:
                    var chatHttpClient = new HttpClient(ActivatorUtilities.CreateInstance<OpenAIHttpClientHandler>(_serviceProvider, chatModel.EndPoint));
                    builder.AddOpenAIChatCompletion(
                       modelId: chatModel.ModelName,
                       apiKey: chatModel.ModelKey,
                       chatModel.ModelDescription,
                       httpClient: chatHttpClient);
                    break;

                case AIType.Ollama:
                    var ollamaHttpClient = new HttpClient(ActivatorUtilities.CreateInstance<OllamaHttpClientHandler>(_serviceProvider, chatModel.EndPoint));
                    builder.AddOpenAIChatCompletion(
                    modelId: chatModel.ModelName,
                    apiKey: chatModel.ModelKey,
                    chatModel.ModelDescription,
                    httpClient: ollamaHttpClient);
                    break;

                case Model.Enum.AIType.AzureOpenAI:
                    builder.AddAzureOpenAIChatCompletion(
                        deploymentName: chatModel.ModelName,
                        apiKey: chatModel.ModelKey,
                        serviceId: chatModel.ModelDescription,
                        endpoint: chatModel.EndPoint);
                    break;

                case Model.Enum.AIType.LLamaSharp:
                    var (weights, parameters) = LLamaConfig.GetLLamaConfig(chatModel.ModelName);
                    var ex = new StatelessExecutor(weights, parameters);
                    builder.Services.AddKeyedSingleton<ITextGenerationService>(chatModel.ModelDescription, new LLamaSharpTextCompletion(ex));
                    builder.Services.AddKeyedSingleton<IChatCompletionService>(chatModel.ModelDescription, new LLamaSharpChatCompletion(ex));
                    break;

                case Model.Enum.AIType.SparkDesk:
                    var options = new SparkDeskOptions { AppId = chatModel.EndPoint, ApiSecret = chatModel.ModelKey, ApiKey = chatModel.ModelName, ModelVersion = Sdcb.SparkDesk.ModelVersion.V3_5 };
                    builder.Services.AddKeyedSingleton<ITextGenerationService>(chatModel.ModelDescription, new SparkDeskTextCompletion(options, app.Id.ToString()));
                    break;

                case Model.Enum.AIType.DashScope:
                    builder.Services.AddDashScopeChatCompletion(chatModel.ModelKey, chatModel.ModelName, chatModel.ModelDescription);
                    break;

                case Model.Enum.AIType.Claude:
                    var claudeClient = new HttpClient();
                    builder.AddOpenAIChatCompletion(
                        modelId: chatModel.ModelName,
                        apiKey: chatModel.ModelKey,
                        chatModel.ModelDescription,
                        httpClient: claudeClient);
                    break;

                case Model.Enum.AIType.Gemini:
                    var geminiClient = new HttpClient();
                    builder.AddOpenAIChatCompletion(
                        modelId: chatModel.ModelName,
                        apiKey: chatModel.ModelKey,
                        chatModel.ModelDescription,
                        httpClient: geminiClient);
                    break;

                case Model.Enum.AIType.Mock:
                    //builder.Services.AddKeyedSingleton<ITextGenerationService>(chatModel.ModelDescription, new MockTextCompletion());
                    builder.Services.AddKeyedSingleton<IChatCompletionService>(chatModel.ModelDescription, new MockTextCompletion());
                    break;
            }
        }

        /// <summary>
        /// Import plugins based on the app configuration
        /// </summary>
        /// <param name="app"></param>
        /// <param name="_kernel"></param>
        public async Task ImportFunctionsByApp(Apps app, Kernel _kernel)
        {
            // avoid registering plugins multiple times
            if (_kernel.Plugins.Any(p => p.Name == "SigmaFunctions"))
            {
                return;
            }
            List<KernelFunction> pluginFunctions = new List<KernelFunction>();

            // API plugins
            if (!string.IsNullOrWhiteSpace(app.PluginList))
            {
                // enable automatic plugin invocation
                var plguinIdList = app.PluginList.Split(",");
                var plguinList = _pluginRepository.GetList(p => plguinIdList.Contains(p.Id));

                foreach (var plug in plguinList)
                {
                    if(!Uri.TryCreate(plug.Url, UriKind.Absolute, out var validated) || validated.Scheme != Uri.UriSchemeHttps)
                    {
                        continue;
                    }
                    if (plug.Type == PluginType.OpenAPI)
                    {
                        var openApi = await _kernel.CreatePluginFromOpenApiAsync(plug.Name, new Uri(plug.Url), new()
                        {
                            //AuthCallback = (request, _) =>
                            //{
                            //    request.Headers.Add("Authorization", "Bearer XXX");
                            //    return Task.CompletedTask;
                            //}
                        });

                        pluginFunctions.AddRange(openApi);
                        continue;
                    }

                    switch (plug.Method)
                    {
                        case HttpMethodType.Get:
                            pluginFunctions.Add(_kernel.CreateFunctionFromMethod((string msg) =>
                            {
                                try
                                {
                                    Console.WriteLine(msg);
                                    RestClient client = new RestClient();
                                    RestRequest request = new RestRequest(plug.Url, Method.Get);
                                    foreach (var header in plug.Header.ConvertToString().Split("\n"))
                                    {
                                        var headerArray = header.Split(":");
                                        if (headerArray.Length == 2)
                                        {
                                            request.AddHeader(headerArray[0], headerArray[1]);
                                        }
                                    }
                                    // TODO: handle parameter extraction in a later iteration
                                    foreach (var query in plug.Query.ConvertToString().Split("\n"))
                                    {
                                        var queryArray = query.Split("=");
                                        if (queryArray.Length == 2)
                                        {
                                            request.AddQueryParameter(queryArray[0], queryArray[1]);
                                        }
                                    }
                                    var result = client.Execute(request);
                                    return result.Content;
                                }
                                catch (System.Exception ex)
                                {
                                    return "Invocation failed: " + ex.Message;
                                }
                            }, plug.Name, $"{plug.Describe}"));
                            break;

                        case HttpMethodType.Post:
                            pluginFunctions.Add(_kernel.CreateFunctionFromMethod((string msg) =>
                            {
                                try
                                {
                                    Console.WriteLine(msg);
                                    RestClient client = new RestClient();
                                    RestRequest request = new RestRequest(plug.Url, Method.Post);
                                    foreach (var header in plug.Header.ConvertToString().Split("\n"))
                                    {
                                        var headerArray = header.Split(":");
                                        if (headerArray.Length == 2)
                                        {
                                            request.AddHeader(headerArray[0], headerArray[1]);
                                        }
                                    }
                                    // TODO: handle parameter extraction in a later iteration
                                    request.AddJsonBody(plug.JsonBody.ConvertToString());
                                    var result = client.Execute(request);
                                    return result.Content;
                                }
                                catch (System.Exception ex)
                                {
                                    return "Invocation failed: " + ex.Message;
                                }
                            }, plug.Name, $"{plug.Describe}"));
                            break;
                    }
                }
            }

            // local function plugins
            if (!string.IsNullOrWhiteSpace(app.NativeFunctionList)) // need to check whether the app enabled local function plugins
            {
                var nativeIdList = app.NativeFunctionList.Split(",");

                _functionService.SearchMarkedMethods();

                foreach (var func in _functionService.Functions)
                {
                    if (nativeIdList.Contains(func.Key))
                    {
                        var methodInfo = _functionService.MethodInfos[func.Key];
                        var parameters = methodInfo.Parameters.Select(x => new KernelParameterMetadata(x.ParameterName) { ParameterType = x.ParameterType, Description = x.Description });
                        var returnType = new KernelReturnParameterMetadata() { ParameterType = methodInfo.ReturnType.ParameterType, Description = methodInfo.ReturnType.Description };
                        var target = ActivatorUtilities.CreateInstance(_serviceProvider, func.Value.DeclaringType);
                        pluginFunctions.Add(_kernel.CreateFunctionFromMethod(func.Value, target, func.Key, methodInfo.Description, parameters, returnType));
                    }
                }
            }
            _kernel.ImportPluginFromFunctions("SigmaFunctions", pluginFunctions);
        }

        /// <summary>
        /// Register default plugins
        /// </summary>
        /// <param name="kernel"></param>
        private void RegisterPluginsWithKernel(Kernel kernel)
        {
            kernel.ImportPluginFromObject(new ConversationSummaryPlugin(), "ConversationSummaryPlugin");
            //kernel.ImportPluginFromObject(new TimePlugin(), "TimePlugin");
            kernel.ImportPluginFromPromptDirectory(Path.Combine(RepoFiles.SamplePluginsPath(), "KMSPlugin"));
            kernel.ImportPluginFromPromptDirectory(Path.Combine(RepoFiles.SamplePluginsPath(), "CyberIntelPlugin"));
        }

        /// <summary>
        /// Conversation summary
        /// </summary>
        /// <param name="_kernel"></param>
        /// <param name="questions"></param>
        /// <param name="history"></param>
        /// <returns></returns>
        public async Task<string> HistorySummarize(Kernel _kernel, string questions, string history)
        {
            KernelFunction sunFun = _kernel.Plugins.GetFunction("ConversationSummaryPlugin", "SummarizeConversation");
            var summary = await _kernel.InvokeAsync(sunFun, new() { ["input"] = $"Content: {history.ToString()} {Environment.NewLine} Please summarize in Chinese" });
            string his = summary.GetValue<string>();
            var msg = $"history：{history.ToString()}{Environment.NewLine} user：{questions}"; ;
            return msg;
        }
    }
}