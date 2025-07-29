namespace Sigma.Core.Options
{
    public class LLamaSharpOption
    {
        public string RunType { get; set; } = "CPU";
        public string Chat { get; set; } = string.Empty;

        public string Embedding { get; set; } = string.Empty;

        public string FileDirectory { get; set; } = Directory.GetCurrentDirectory();
    }
}
