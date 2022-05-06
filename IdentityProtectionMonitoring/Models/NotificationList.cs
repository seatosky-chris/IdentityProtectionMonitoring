using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityProtectionMonitoring.Models
{
    // Class representing an array of notifications
    // in a notification payload
    public class NotificationList
    {
        public ChangeNotificationPayload[] Value { get; set; }
    }
}
