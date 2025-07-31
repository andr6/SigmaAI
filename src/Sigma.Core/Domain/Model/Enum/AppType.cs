using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sigma.Core.Domain.Model.Enum
{
    public enum AppType
    {
        [Display(Name = "Chat Application")]
        Chat = 1,

        [Display(Name = "Knowledge Base")]
        Kms = 2
    }
}
