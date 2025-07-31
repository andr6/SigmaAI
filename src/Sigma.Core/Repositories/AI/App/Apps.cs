using Sigma.Core.Repositories.Base;
using Sigma.Core.Domain.Model.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sigma.Core.Repositories
{
    [Table("Apps")]
    public partial class Apps : EntityBase
    {
        /// <summary>
        /// Name
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        [Required]
        public string Describe { get; set; }

        /// <summary>
        /// Icon
        /// </summary>
        [Required]
        public string Icon { get; set; } = "appstore";

        /// <summary>
        /// Type
        /// </summary>
        [Required]
        public AppType Type { get; set; } = AppType.Chat;

        /// <summary>
        /// Chat model ID
        /// </summary>
        public string? ChatModelID { get; set; }

        /// <summary>
        /// Embedding model ID
        /// </summary>
        public string? EmbeddingModelID { get; set; }

        /// <summary>
        /// Temperature
        /// </summary>
        public double Temperature { get; set; } = 70f;

        /// <summary>
        /// Prompt
        /// </summary>
        public string? Prompt { get; set; }

        /// <summary>
        /// Plugin list
        /// </summary>
        [Column(TypeName = "varchar(1000)")]
        public string? PluginList { get; set; }

        /// <summary>
        /// Native function list
        /// </summary>
        [Column(TypeName = "varchar(1000)")]
        public string? NativeFunctionList { get; set; }

        /// <summary>
        /// Knowledge base ID list
        /// </summary>
        public string? KmsIdList { get; set; }

        /// <summary>
        /// API secret key
        /// </summary>
        public string? SecretKey { get; set; }

        [NotMapped]
        public AIModels AIModel { get; set; }
    }
}