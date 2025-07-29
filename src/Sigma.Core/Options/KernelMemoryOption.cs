namespace Sigma.Core.Options
{
    public class KernelMemoryOption
    {
        /// <summary>
        /// 向量库
        /// </summary>
        public string VectorDb { get; set; } = "Memory";
        /// <summary>
        /// 连接字符串
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;
        /// <summary>
        /// 表前缀
        /// </summary>
        public string TableNamePrefix { get; set; } = string.Empty;
    }
}
