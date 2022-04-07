using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;
using System.Threading.Tasks;

namespace fbognini.WebFramework.Filters
{
    public class MultiplePolicysAuthorizeAttribute : TypeFilterAttribute
    {
        public string[] Policys { get; private set; }
        public bool IsAnd { get; private set; }

        public MultiplePolicysAuthorizeAttribute(string[] policys, bool isAnd = false) : base(typeof(MultiplePolicysAuthorizeFilter))
        {
            Policys = policys;
            IsAnd = isAnd;

            Arguments = new object[] { policys, isAnd };
        }
    }

    internal class MultiplePolicysAuthorizeFilter : IAsyncAuthorizationFilter
    {
        private readonly IAuthorizationService _authorization;
        public string[] Policys { get; private set; }
        public bool IsAnd { get; private set; }

        public MultiplePolicysAuthorizeFilter(string[] policys, bool isAnd, IAuthorizationService authorization)
        {
            Policys = policys;
            IsAnd = isAnd;
            _authorization = authorization;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var policys = Policys.ToList();
            if (IsAnd)
            {
                foreach (var policy in policys)
                {
                    var authorized = await _authorization.AuthorizeAsync(context.HttpContext.User, policy);
                    if (!authorized.Succeeded)
                    {
                        context.Result = new ForbidResult();
                        return;
                    }

                }
            }
            else
            {
                foreach (var policy in policys)
                {
                    var authorized = await _authorization.AuthorizeAsync(context.HttpContext.User, policy);
                    if (authorized.Succeeded)
                    {
                        return;
                    }

                }
                context.Result = new ForbidResult();
                return;
            }
        }
    }

}
