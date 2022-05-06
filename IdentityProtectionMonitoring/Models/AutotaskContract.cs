using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityProtectionMonitoring.Models
{
    // A class representing a contract returned from the Autotask API
    public class AutotaskContract
    {
        public int Id { get; set; }
    }

    public class AutotaskContractsList
    {
        public List<AutotaskContract>? Items { get; set; }
        public AutotaskContract? Item { get; set; }
        public AutotaskPageDetails? PageDetails { get; set; }
    }
}