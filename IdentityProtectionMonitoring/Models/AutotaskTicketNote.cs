using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityProtectionMonitoring.Models
{
    // A class representing a payload for a new ticket note for the Autotask API
    public class AutotaskTicketNote
    {
        public string Description { get; set; }
        public int NoteType { get; set; } = 1;
        public int Publish { get; set; } = 1;
        public int TicketID { get; set; }
        public string? Title { get; set; }
    }
}
