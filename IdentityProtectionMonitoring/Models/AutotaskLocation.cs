using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityProtectionMonitoring.Models
{
    // A class representing a location returned from the Autotask API
    public class AutotaskLocation
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }
        public bool IsPrimary { get; set; }
    }

    public class AutotaskLocationsList
    {
        public List<AutotaskLocation>? Items { get; set; }
        public AutotaskLocation? Item { get; set; }
        public AutotaskPageDetails? PageDetails { get; set; }
    }
}
