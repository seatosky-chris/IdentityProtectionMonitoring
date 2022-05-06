using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityProtectionMonitoring.Models
{
    public class AlertDetails
    {
        public string UserDisplayName { get; set; }
        public string Username { get; set; }
        public string AadUserID { get; set; }
        public string? UserRiskLevel { get; set; }
        public string? UserRiskDetails { get; set; }
        public string? UserRiskReportId { get; set; }
        public string AlertTitle { get; set; }
        public string AlertCategory { get; set; }
        public string AlertDescription { get; set; }
        public DateTimeOffset? AlertEventDateTime { get; set; }
        public string? AlertSeverity { get; set; }
        public string? Activity { get; set; }
        public string IpAddress { get; set; }
        public string Location { get; set; }
        public string RiskEventType { get; set; }
        public string Source { get; set; }
        public string AdditionalInfo { get; set; }
    }
}
