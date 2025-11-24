using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.Entities
{
    public static class EntityConstants
    {
        public const int KeyLength = 36; // Guid string uzunluğu genelde 36'dır
        public const int DisplayNameLength = 250;
    }

    public class EntityBase : IEntityBase
    {
        [Key]
        [StringLength(EntityConstants.KeyLength)]
        public string Id { get; set; } = Guid.NewGuid().ToString(); // Otomatik ID

        [StringLength(EntityConstants.DisplayNameLength)]
        public string CreatedByName { get; set; } = string.Empty;

        [StringLength(EntityConstants.KeyLength)]
        public string? CreatedById { get; set; }

        [StringLength(EntityConstants.DisplayNameLength)]
        public string? ModifiedByName { get; set; }

        [StringLength(EntityConstants.KeyLength)]
        public string? ModifiedById { get; set; }

        [StringLength(EntityConstants.DisplayNameLength)]
        public string? RemovedByName { get; set; }

        [StringLength(EntityConstants.KeyLength)]
        public string? RemovedById { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Default değer
        public DateTime? ModifiedAt { get; set; }
        public DateTime? RemovedAt { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}
