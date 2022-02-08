using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graph.Module
{
    public class NewUserModel
    {
        public string GivenName { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string UserName { get { return Email; } }
        public bool IsAdminRole { get; set; }
        public string Id { get; set; }
        public string DisplayName
        {
            get
            {
                return String.IsNullOrEmpty(GivenName) ? UserName : String.Format("{0} {1}", GivenName, Surname);
            }
        }

        public bool IsNewUser { get; set; }
    }
}
