using System.ComponentModel.DataAnnotations;

namespace Sigma.Core.Domain.Model.Enum
{
    /// <summary>
    /// AI type
    /// </summary>
    public enum AIType
    {
        [Display(Name = "Open AI Compatible")]
        OpenAI = 1,

        [Display(Name = "Azure Open AI")]
        AzureOpenAI = 2,

        [Display(Name = "LLama Local Model")]
        LLamaSharp = 3,

        [Display(Name = "SparkDesk Model")]
        SparkDesk = 4,

        [Display(Name = "DashScope Model")]
        DashScope = 5,

        [Display(Name = "Ollama")]
        Ollama = 6,

        [Display(Name = "Anthropic Claude")]
        Claude = 7,

        [Display(Name = "Google Gemini")]
        Gemini = 8,

        [Display(Name = "Mock Output")]
        Mock = 100,
     }

    /// <summary>
    /// Model type
    /// </summary>
    public enum AIModelType
    {
        Chat = 1,
        Embedding = 2,
    }
}