using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrmServiceIntegration
{
    public static  class CRMConstants
    {
       public static class Address
        {
            public const string AddressLine1 = "line1";
            public const string City = "city";
            public const string Country = "country";
        }

        public static class Account
        {
            public const string EntityName = "account";
        }

        public static class Contact
        {
            public const string EntityName = "contact";
            public const string ParentCustomerId = "parentcustomerid";
        }
    }
}
