namespace Sigma.Core.Domain.Model
{
    public class PageList<T>
    {
        // query results
        public List<T> List { get; set; }
        /// <summary>
        /// Current page starting from 1
        /// </summary>
        public int PageIndex { get; set; }
        /// <summary>
        /// Page size
        /// </summary>
        public int PageSize { get; set; }
        /// <summary>
        /// Total count
        /// </summary>
        public int TotalCount { get; set; }
    }
}
