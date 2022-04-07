using System.Collections.Generic;
using System.Linq;

namespace fbognini.WebFramework.SidebarMenu
{
    public class PolicyClass
    {
        public PolicyClass(IEnumerable<string> policys, bool isAnd)
        {
            Policys = policys;
            IsAnd = isAnd;
        }

        public IEnumerable<string> Policys { get; set; }
        public bool IsAnd { get; set; }

        public bool IsPolicysValid(IEnumerable<string> policys)
        {
            if (Policys?.Any() == false)
                return true;

            if (IsAnd)
            {
                return Policys.All(x => policys.Any(y => y == x));
            }
            else
            {
                return Policys.Any(x => policys.Any(y => y == x));
            }
        }
    }

}
