using System.Collections.Generic;
using System.Linq;

namespace fbognini.WebFramework.DynamicMenu
{
    public class ActionAuthorizationPolicy
    {
        public ActionAuthorizationPolicy(IEnumerable<string> policys, bool isAnd)
        {
            Policys = policys;
            IsAnd = isAnd;
        }

        public IEnumerable<string> Policys { get; set; }
        public bool IsAnd { get; set; }

        public bool IsPolicysValid(IEnumerable<string> policys)
        {
            if (Policys is null || !Policys.Any())
            {
                return true;
            }

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
