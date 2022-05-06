using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace IdentityProtectionMonitoring.Models
{
    public class DBTicketReference
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public string UserPrincipalName {  get; set; }
        public string AlertCategory { get; set; }
        public string Source { get; set; }
        public string? RiskyUserId { get; set; }
        public string AadUserID { get; set; }
        public int TicketId { get; set; }
        public DateTime Created { get; set; }
    }
}
