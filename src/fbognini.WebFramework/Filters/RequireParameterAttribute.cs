using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Routing;
using System.Linq;

namespace fbognini.WebFramework.Filters
{
    /// Flags an Action Method valid for any incoming request only if all, any or none of the given HTTP parameter(s) are set,
    /// enabling the use of multiple Action Methods with the same name (and different signatures) within the same MVC Controller.
    /// </summary>
    public class RequireParameterAttribute : ActionMethodSelectorAttribute
    {
        public RequireParameterAttribute(string parameterName, MatchMode matchMode = MatchMode.All) : this(matchMode, parameterName.Split(',', System.StringSplitOptions.RemoveEmptyEntries))
        {
        }

        public RequireParameterAttribute(MatchMode matchMode, params string[] parameterNames)
        {
            ParameterNames = parameterNames;
            IncludeGET = true;
            IncludePOST = true;
            IncludeCookies = false;
            Mode = matchMode;
        }

        public override bool IsValidForRequest(RouteContext routeContext, ActionDescriptor action)
        {
            switch (Mode)
            {
                case MatchMode.All:
                default:
                    return (
                        (IncludeGET && ParameterNames.All(p => routeContext.HttpContext.Request.Query?.ContainsKey(p) == true))
                        || (IncludePOST && routeContext.HttpContext.Request.ContentType?.Contains("application/x-www-form-urlencoded") == true && ParameterNames.All(p => routeContext.HttpContext.Request.Form?.ContainsKey(p) == true))
                        || (IncludeCookies && ParameterNames.All(p => routeContext.HttpContext.Request.Cookies?.ContainsKey(p) == true))
                        );
                case MatchMode.Any:
                    return (
                        (IncludeGET && ParameterNames.Any(p => routeContext.HttpContext.Request.Query.ContainsKey(p)))
                        || (IncludePOST && ParameterNames.Any(p => routeContext.HttpContext.Request.Form.ContainsKey(p)))
                        || (IncludeCookies && ParameterNames.Any(p => routeContext.HttpContext.Request.Cookies.ContainsKey(p)))
                        );
                case MatchMode.None:
                    return (
                        (!IncludeGET || !ParameterNames.Any(p => routeContext.HttpContext.Request.Query.ContainsKey(p)))
                        && (!IncludePOST || !ParameterNames.Any(p => routeContext.HttpContext.Request.Form.ContainsKey(p)))
                        && (!IncludeCookies || !ParameterNames.Any(p => routeContext.HttpContext.Request.Cookies.ContainsKey(p)))
                        );
            }
        }

        public string[] ParameterNames { get; private set; }

        /// <summary>
        /// Set it to TRUE to include GET (QueryStirng) parameters, FALSE to exclude them:
        /// default is TRUE.
        /// </summary>
        public bool IncludeGET { get; set; }

        /// <summary>
        /// Set it to TRUE to include POST (Form) parameters, FALSE to exclude them:
        /// default is TRUE.
        /// </summary>
        public bool IncludePOST { get; set; }

        /// <summary>
        /// Set it to TRUE to include parameters from Cookies, FALSE to exclude them:
        /// default is FALSE.
        /// </summary>
        public bool IncludeCookies { get; set; }

        /// <summary>
        /// Use MatchMode.All to invalidate the method unless all the given parameters are set (default).
        /// Use MatchMode.Any to invalidate the method unless any of the given parameters is set.
        /// Use MatchMode.None to invalidate the method unless none of the given parameters is set.
        /// </summary>
        public MatchMode Mode { get; set; }

        public enum MatchMode : int
        {
            All,
            Any,
            None
        }
    }
}
