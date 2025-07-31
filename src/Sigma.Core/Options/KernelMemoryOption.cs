namespace Sigma.Core.Options
{
    public class KernelMemoryOption
    {
        /// <summary>
        /// Vector database type
        /// </summary>
        public string VectorDb { get; set; } = "Memory";
        /// <summary>
        /// Connection string
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;
        /// <summary>
        /// Table name prefix
        /// </summary>
        public string TableNamePrefix { get; set; } = string.Empty;
    }
}
