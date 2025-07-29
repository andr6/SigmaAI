﻿using Sigma.LLM.SparkDesk;
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
        /// 获取kernel实例，依赖注入不好按每个用户去Import不同的插件，所以每次new一个新的kernel
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
        /// 根据app配置的插件，导入插件
        /// </summary>
        /// <param name="app"></param>
        /// <param name="_kernel"></param>
        public async Task ImportFunctionsByApp(Apps app, Kernel _kernel)
        {
            //插件不能重复注册，否则会异常
            if (_kernel.Plugins.Any(p => p.Name == "SigmaFunctions"))
            {
                return;
            }
            List<KernelFunction> pluginFunctions = new List<KernelFunction>();

            //API插件
            if (!string.IsNullOrWhiteSpace(app.PluginList))
            {
                //开启自动插件调用
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
                                    //这里应该还要处理一次参数提取，等后面再迭代
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
                                    return "调用失败：" + ex.Message;
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
                                    //这里应该还要处理一次参数提取，等后面再迭代
                                    request.AddJsonBody(plug.JsonBody.ConvertToString());
                                    var result = client.Execute(request);
                                    return result.Content;
                                }
                                catch (System.Exception ex)
                                {
                                    return "调用失败：" + ex.Message;
                                }
                            }, plug.Name, $"{plug.Describe}"));
                            break;
                    }
                }
            }

            //本地函数插件
            if (!string.IsNullOrWhiteSpace(app.NativeFunctionList))//需要添加判断应用是否开启了本地函数插件
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
        /// 注册默认插件
        /// </summary>
        /// <param name="kernel"></param>
        private void RegisterPluginsWithKernel(Kernel kernel)
        {
            kernel.ImportPluginFromObject(new ConversationSummaryPlugin(), "ConversationSummaryPlugin");
            //kernel.ImportPluginFromObject(new TimePlugin(), "TimePlugin");
            kernel.ImportPluginFromPromptDirectory(Path.Combine(RepoFiles.SamplePluginsPath(), "KMSPlugin"));
        }

        /// <summary>
        /// 会话总结
        /// </summary>
        /// <param name="_kernel"></param>
        /// <param name="questions"></param>
        /// <param name="history"></param>
        /// <returns></returns>
        public async Task<string> HistorySummarize(Kernel _kernel, string questions, string history)
        {
            KernelFunction sunFun = _kernel.Plugins.GetFunction("ConversationSummaryPlugin", "SummarizeConversation");
            var summary = await _kernel.InvokeAsync(sunFun, new() { ["input"] = $"内容是：{history.ToString()} {Environment.NewLine} 请注意用中文总结" });
            string his = summary.GetValue<string>();
            var msg = $"history：{history.ToString()}{Environment.NewLine} user：{questions}"; ;
            return msg;
        }
    }
}