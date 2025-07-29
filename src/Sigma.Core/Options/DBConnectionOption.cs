namespace Sigma.Core.Options
{
    public class DBConnectionOption
    {
        /// <summary>
        /// sqlite连接字符串
        /// </summary>
        public string DbType { get; set; } = string.Empty;
        /// <summary>
        /// pg链接字符串
        /// </summary>
        public string ConnectionStrings { get; set; } = string.Empty;
    }
}
