using Sigma.Core.Repositories.Base;
using Sigma.Core.Domain.Model.Enum;
using Sigma.Core.Repositories.AI.Plugin;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sigma.Core.Repositories
{
    public partial class Plugin : EntityBase
    {
        /// <summary>
        /// API name
        /// </summary>
        [Required]
        public string Name { get; set; }

        [Required]
        public PluginType Type { get; set; } = PluginType.OpenAPI;

        /// <summary>
        /// API description
        /// </summary>
        [Required]
        public string Describe { get; set; }

        /// <summary>
        /// API url
        /// </summary>
        [Required]
        public string Url { get; set; }

        /// <summary>
        /// HTTP method
        /// </summary>
        public HttpMethodType Method { get; set; } = HttpMethodType.Get;

        [Column(TypeName = "varchar(1000)")]
        public string? Header { get; set; }

        /// <summary>
        /// Query string parameters
        /// </summary>
        [Column(TypeName = "varchar(1000)")]
        public string? Query { get; set; }

        /// <summary>
        /// json body payload
        /// </summary>
        [Column(TypeName = "varchar(7000)")]
        public string? JsonBody { get; set; }

        /// <summary>
        /// input prompt
        /// </summary>
        [Column(TypeName = "varchar(1500)")]
        public string? InputPrompt { get; set; }

        /// <summary>
        /// output prompt
        /// </summary>
        [Column(TypeName = "varchar(1500)")]
        public string? OutputPrompt { get; set; }
    }
}