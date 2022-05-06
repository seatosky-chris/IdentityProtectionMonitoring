using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityProtectionMonitoring.Models
{
    public class From
    {
        public string Email { get; set; }
        public string Name { get; set; } = "";
    }

    public class To
    {
        public string Email { get; set; }
        public string Name { get; set; } = "";
    }

    public class NewEmail
    {
        public From From { get; set; }
        public IList<To> To { get; set; }
        public string Subject { get; set; }
        public string HTMLContent { get; set; }
    }


}
