using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.DTOs
{
    public class AssignRolesDto
    {
        [Required]
        public List<string> RoleNames { get; set; } = new List<string>();
    }
}
