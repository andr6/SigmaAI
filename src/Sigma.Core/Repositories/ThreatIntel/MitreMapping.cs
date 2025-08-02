using System.ComponentModel.DataAnnotations;
using Sigma.Core.Repositories.Base;

namespace Sigma.Core.Repositories.ThreatIntel
{
    public class MitreMapping : EntityBase
    {
        [Required]
        public string TechniqueId { get; set; }
    }
}
