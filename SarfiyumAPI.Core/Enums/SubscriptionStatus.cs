using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SarfiyumAPI.Core.Enums
{
    public enum SubscriptionStatus
    {
        Active = 1,   // Şu an geçerli
        Expired = 2,  // Süresi dolmuş
        Cancelled = 3 // İptal edilmiş
    }
}
