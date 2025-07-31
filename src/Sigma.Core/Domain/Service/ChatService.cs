using Sigma.Core.Domain.Interface;
using Sigma.Core.Repositories;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel;
using System.Text;
using Sigma.Core.Utils;
using Sigma.Core.Domain.Model.Dto;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using LLMJson;
using Sigma.Core.OutputParsers;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Sigma.Core.Domain.Service
{
    public class ChatService(
        IKernelService _kernelService,
        IKMService _kMService,
        IKmsDetails_Repositories _kmsDetails_Repositories,
        IModelMetricsService _metrics
        ) : IChatService
    {
        private JsonSerializerOptions JsonSerializerOptions = new()
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };

        public ChatHistory GetChatHistory(List<Chat.ChatHistory> histories)
        {
            ChatHistory history = [];
            if (histories.Count > 1)
            {
                foreach (var item in histories)
                {
                    if (item.Role== Chat.ChatRoles.User)
                    {
                        history.AddUserMessage(item.Content);
                    }
                    else
                    {
                        history.AddAssistantMessage(item.Content);
                    }
                }
            }
            return history;
        }

        /// <summary>
        /// Send message
        /// </summary>
        /// <param name="app"></param>
        /// <param name="questions"></param>
        /// <param name="history"></param>
        /// <returns></returns>
        public async IAsyncEnumerable<StreamingKernelContent> SendChatByAppAsync(Apps app, string questions, ChatHistory history)
        {
            var _kernel = _kernelService.GetKernelByApp(app);
            var temperature = app.Temperature / 100; // value is 0-100 so scale down
            OpenAIPromptExecutionSettings settings = new() { Temperature = temperature };
            var useIntentionRecognition = app.AIModel?.UseIntentionRecognition == true;

            if (!string.IsNullOrEmpty(app.PluginList) || !string.IsNullOrEmpty(app.NativeFunctionList)) // need to include local plugins
            {
                await _kernelService.ImportFunctionsByApp(app, _kernel);
                settings.ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions;
            }

            if (string.IsNullOrEmpty(app.Prompt) || !app.Prompt.Contains("{{$input}}"))
            {
                // use default prompt if template is empty
                app.Prompt = app.Prompt?.ConvertToString() + "{{$input}}";
            }

            var prompt = app.Prompt;

            if (useIntentionRecognition)
            {
                prompt = GenerateFuncionPrompt(_kernel) + prompt;
            }

            var start = DateTime.UtcNow;
            await foreach (var content in Execute())
                yield return content;
            await _metrics.LogUsageAsync(app.Name, DateTime.UtcNow - start, true);

            async IAsyncEnumerable<StreamingKernelContent> Execute()
            {
                KernelArguments args = [];
                if (history.Count > 10)
                {
                    prompt = @"${{ConversationSummaryPlugin.SummarizeConversation $history}}" + prompt;

                    args.Add("history", string.Join("\n", history.Select(x => x.Role + ": " + x.Content)));
                    args.Add("input", questions);
                }
                else
                {
                    args.Add("input", $"{string.Join("\n", history.Select(x => x.Role + ": " + x.Content))}{Environment.NewLine} user:{questions}");
                }

                var func = _kernel.CreateFunctionFromPrompt(prompt, settings);
                var chatResult = _kernel.InvokeStreamingAsync(function: func, arguments: args);

                if (!useIntentionRecognition)
                {
                    await foreach (var content in chatResult)
                        yield return content;

                    yield break;
                }

                var result = "";
                var successMatch = false;

                List<StreamingKernelContent> contentBuffer = [];

                await foreach (var content in chatResult)
                {
                    result += content.ToString();
                    successMatch = result.Contains("func", StringComparison.InvariantCultureIgnoreCase);

                    // wait for function until the result lenght is more than 20
                    if (result.Length > 20 && !successMatch)
                    {
                        if (contentBuffer.Count > 0)
                        {
                            foreach (var c in contentBuffer)
                                yield return c;

                            contentBuffer.Clear();
                        }

                        yield return content;
                    }
                    else
                    {
                        contentBuffer.Add(content);
                    }
                }

                if (!successMatch)
                {
                    foreach (var c in contentBuffer)
                        yield return c;

                    yield break;
                }

                var callResult = new List<string>();

                try
                {
                    var functioResults = JsonParser.FromJson<List<FunctionSchema>>(result);

                    foreach (var functioResult in functioResults)
                    {
                        var plugin = _kernel?.Plugins.GetFunctionsMetadata().Where(x => x.PluginName == "SigmaFunctions").ToList().FirstOrDefault(f => f.Name == functioResult.Function);
                        if (plugin == null)
                        {
                            yield break;
                        }

                        if (!_kernel.Plugins.TryGetFunction(plugin.PluginName, plugin.Name, out var function))
                        {
                            yield break;
                        }

                        var parameters = plugin.Parameters.ToDictionary(x => x.Name, x => x.ParameterType!);
                        var arguments = new KernelArguments(JsonParameterParser.ParseJsonToDictionary(functioResult.Arguments, parameters));

                        var funcResult = (await function.InvokeAsync(_kernel, arguments)).GetValue<object>() ?? string.Empty;
                        callResult.Add($"- {functioResult.Reason}, result: {JsonSerializer.Serialize(funcResult, JsonSerializerOptions)}");
                    }
                }
                catch (Exception e)
                {
                    callResult.Add($"Function invocation threw an exception: {e.Message}");
                }

                history = new ChatHistory($"""
                    system: Summarize the response based on user intent and the following results.

                    Known intents and results:

                    {string.Join("\r\n\r\n", callResult)}.

                    Please answer the user's last question using this information:
                    """);

                //questions = "Please reword this result";
                prompt = "{{$input}}";
                useIntentionRecognition = false;

                await foreach (var content in Execute())
                    yield return content;
            }
        }

        public async IAsyncEnumerable<StreamingKernelContent> SendKmsByAppAsync(Apps app, string questions, ChatHistory history, List<RelevantSource> relevantSources = null)
        {
            var _kernel = _kernelService.GetKernelByApp(app);
            var relevantSourceList = await _kMService.GetRelevantSourceList(app.KmsIdList, questions);
            var dataMsg = new StringBuilder();
            if (relevantSourceList.Any())
            {
                relevantSources?.AddRange(relevantSourceList);
                foreach (var item in relevantSources)
                {
                    dataMsg.AppendLine(item.ToString());
                }
                KernelFunction jsonFun = _kernel.Plugins.GetFunction("KMSPlugin", "Ask");
                var chatResult = _kernel.InvokeStreamingAsync(function: jsonFun,
                    arguments: new KernelArguments() { ["doc"] = dataMsg, ["history"] = history, ["questions"] = questions });

                await foreach (var content in chatResult)
                {
                    yield return content;
                }
            }
            else
            {
                yield return new StreamingTextContent("No related content found in the knowledge base");
            }
        }


        private string GenerateFuncionPrompt(Kernel kernel)
        {
            var functions = kernel?.Plugins.GetFunctionsMetadata().Where(x => x.PluginName == "SigmaFunctions").ToList() ?? [];
            if (!functions.Any())
                return "";

            var functionNames = functions.Select(x => x.Description).ToList();
            var functionKV = functions.ToDictionary(x => x.Description, x => new { Function = $"{x.Name}", Parameters = x.Parameters.Select(x => $"{x.Name}:{TypeParser.ConvertToTypeScriptType(x.ParameterType!)} // {x.Description},{(TypeParser.IsArrayOrList(x.ParameterType!) ? "multiple" : "single")}") });
            var template = $$"""
                          Perform intent recognition for the user's last question.

                          Known intents are

                          {{JsonSerializer.Serialize(functionNames, JsonSerializerOptions)}}

                          The corresponding functions are:

                          {{JsonSerializer.Serialize(functionKV, JsonSerializerOptions)}}

                          Identify one or more intents from the list and output only the JSON object below without markdown or extra text.

                          Output format:

                          [{
                             "function": string   // function name for the intent
                             "intention": string  // user intent
                             "arguments": object  // argument values
                             "reason": string     // reason for using these values
                          },{
                             "function": string   // function name for the intent
                             "intention": string  // user intent
                             "arguments": object  // argument values
                             "reason": string     // reason for using these values
                          }]

                          If the user's intent cannot be identified, answer the question directly in markdown without any extra text.
                          """;

            return template;
        }
    }
}