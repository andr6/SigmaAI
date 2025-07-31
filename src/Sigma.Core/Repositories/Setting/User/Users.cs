using Sigma.Core.Repositories.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sigma.Core.Repositories
{
    [Table("Users")]
    public partial class Users : EntityBase
    {
        /// <summary>
        /// Employee number used for login
        /// </summary>
        [Required]
        public string No { get; set; }

        /// <summary>
        /// Password
        /// </summary>
        [Required]
        public string Password { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Notes
        /// </summary>
        [Required]
        public string Describe { get; set; }

        /// <summary>
        /// Menu permissions
        /// </summary>
        [Required]
        public string MenuRole { get; set; }
    }
}