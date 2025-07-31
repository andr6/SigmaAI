using Sigma.Core.Repositories.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sigma.Core.Repositories
{
    [Table("Kms")]
    public partial class Kmss : EntityBase
    {
        /// <summary>
        /// Icon
        /// </summary>
        public string Icon { get; set; } = "appstore";

        /// <summary>
        /// Name
        /// </summary>
        [Required]
        public string Name { get; set; }

        ///// <summary>
        ///// Chat model
        ///// </summary>
        public string Describe { get; set; }

        /// <summary>
        /// Embedding model ID
        /// </summary>
        [Required]
        public string? EmbeddingModelID { get; set; }

        [Required]
        public string? ChatModelId { get; set; }

        /// <summary>
        /// Maximum tokens per paragraph.
        /// </summary>
        public int MaxTokensPerParagraph { get; set; } = 299;

        /// <summary>
        /// Maximum tokens per line
        /// </summary>
        public int MaxTokensPerLine { get; set; } = 99;

        /// <summary>
        /// Overlapping tokens between paragraphs.
        /// </summary>
        public int OverlappingTokens { get; set; } = 49;
    }
}