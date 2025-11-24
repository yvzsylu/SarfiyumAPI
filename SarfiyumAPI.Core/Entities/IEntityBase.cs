using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.Entities
{
    public interface IEntityBase
    {
        string Id { get; set; }
        bool IsDeleted { get; set; }
        // Diğer ortak özellikleri de buraya ekleyebilirsin
    }
}
