using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityProtectionMonitoring.Models
{
    public class ProcessNotificationsPayload
    {
        public string SecretKey { get; set; }
        public NotificationList NotificationList { get; set; }
    }
}
