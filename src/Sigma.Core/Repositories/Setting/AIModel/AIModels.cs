using Sigma.Core.Repositories.Base;
using Sigma.Core.Domain.Model.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sigma.Core.Repositories
{
    [Table("AIModels")]
    public partial class AIModels : EntityBase
    {
        /// <summary>
        /// AI type
        /// </summary>
        [Required]
        public AIType AIType { get; set; } = AIType.OpenAI;

        public bool IsChat { get; set; }

        public bool IsEmbedding { get; set; }

        /// <summary>
        /// Model endpoint
        /// </summary>
        public string EndPoint { get; set; } = "";

        /// <summary>
        /// Model name
        /// </summary>
        public string? ModelName { get; set; } = "";

        /// <summary>
        /// Model key
        /// </summary>
        public string? ModelKey { get; set; } = "";

        /// <summary>
        /// Deployment name (used for Azure)
        /// </summary>
        public string? ModelDescription { get; set; }

        /// <summary>
        /// Enable intent recognition
        /// </summary>
        public bool UseIntentionRecognition { get; set; }
    }
}