using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityProtectionMonitoring.Models
{
    // Class to represent the payload sent to the
    // SetSubscription function
    public class SetAlertsSubscriptionPayload
    {
        // "subscribe" or "unsubscribe"
        public string RequestType { get; set; }

        // If unsubscribing, the subscription to delete
        public string SubscriptionId { get; set; }

        // If subscribing, any filters to apply
        public string Filters { get; set; }

    }
}
