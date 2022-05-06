using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityProtectionMonitoring.Models
{
    // A class representing a contact returned from the Autotask API
    public class AutotaskContact
    {
        public int Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? EmailAddress { get; set; }
        public string? EmailAddress2 { get; set; }
        public string? EmailAddress3 { get; set; }
        public int IsActive { get; set; }
        public DateTime LastActivityDate { get; set; }
    }
    
    public class AutotaskContactsList
    {
        public List<AutotaskContact>? Items { get; set; }
        public AutotaskContact? Item { get; set; }
        public AutotaskPageDetails? PageDetails { get; set; }
    }
}
