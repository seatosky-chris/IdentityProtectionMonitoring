using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityProtectionMonitoring.Models
{
    // Represents a change notification payload
    // https://docs.microsoft.com/graph/api/resources/changenotification?view=graph-rest-1.0
    public class ChangeNotificationPayload
    {
        public string ChangeType { get; set; }
        public string ClientState { get; set; }
        public string Resource { get; set; }
        public ResourceData ResourceData { get; set; }
        public DateTime SubscriptionExpirationDateTime { get; set; }
        public string SubscriptionId { get; set; }
        public string TenantId { get; set; }
    }
}
