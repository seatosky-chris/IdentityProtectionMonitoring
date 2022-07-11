using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityProtectionMonitoring.Models
{
    public class AutotaskQueryFilterItem
    {
        public string op { get; set; }
        public string? field { get; set; }
        public string? value { get; set; }
        public List<AutotaskQueryFilterItem>? items { get; set; }
    }
}
