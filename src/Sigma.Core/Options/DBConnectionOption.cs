namespace Sigma.Core.Options
{
    public class DBConnectionOption
    {
        /// <summary>
        /// SQLite connection string
        /// </summary>
        public string DbType { get; set; } = string.Empty;
        /// <summary>
        /// PostgreSQL connection string
        /// </summary>
        public string ConnectionStrings { get; set; } = string.Empty;
    }
}
