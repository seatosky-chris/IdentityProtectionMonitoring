using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityProtectionMonitoring.Models
{
    // A class representing the pageDetails object returned from the Autotask API
    public class AutotaskPageDetails
    {
        public int Count { get; set; }
        public int RequestCount { get; set; }
        public string? PrevPageUrl { get; set; }
        public string? NextPageUrl { get; set; }
    }
}
