using Sigma.Core.Repositories.Base;
using Sigma.Core.Domain.Model.Enum;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sigma.Core.Repositories
{
    [Table("KmsDetails")]
    public partial class KmsDetails : EntityBase
    {
        public string KmsId { get; set; }

        /// <summary>
        /// File name
        /// </summary>
        public string FileName { get; set; } = "";

        public string FileGuidName { get; set; } = "";

        /// <summary>
        /// Url
        /// </summary>
        public string Url { get; set; } = "";

        /// <summary>
        /// Type file or url
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Data count
        /// </summary>
        public int? DataCount { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        public ImportKmsStatus? Status { get; set; } = ImportKmsStatus.Loadding;
    }
}