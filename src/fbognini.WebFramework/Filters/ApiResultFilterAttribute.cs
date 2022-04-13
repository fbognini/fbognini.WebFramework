using fbognini.Core.Data.Pagination;
using fbognini.WebFramework.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Linq;
using System.Net;

namespace fbognini.WebFramework.Filters
{
    public class ApiResultFilterAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Result is OkObjectResult okObjectResult)
            {
                var apiResult = new ApiResult<object>(true, HttpStatusCode.OK, okObjectResult.Value);
                context.Result = new JsonResult(apiResult) { StatusCode = okObjectResult.StatusCode };
            }
            else if (context.Result is OkResult okResult)
            {
                var apiResult = new ApiResult(true, HttpStatusCode.OK);
                context.Result = new JsonResult(apiResult) { StatusCode = okResult.StatusCode };
            }
            else if (context.Result is BadRequestResult badRequestResult)
            {
                var apiResult = new ApiResult(false, HttpStatusCode.BadRequest);
                context.Result = new JsonResult(apiResult) { StatusCode = badRequestResult.StatusCode };
            }
            else if (context.Result is BadRequestObjectResult badRequestObjectResult)
            {
                if (badRequestObjectResult.Value is SerializableError errors)
                {
                    var errorMessages = errors.SelectMany(p => (string[])p.Value).Distinct();
                    var message = string.Join(" | ", errorMessages);

                    var apiResult = new ApiResult(false, HttpStatusCode.BadRequest, message);
                    context.Result = new JsonResult(apiResult) { StatusCode = badRequestObjectResult.StatusCode };
                }

                if (badRequestObjectResult.Value is ValidationProblemDetails problems)
                {
                    var apiResult = new ApiResult(false, HttpStatusCode.BadRequest, null, problems.Errors);
                    context.Result = new JsonResult(apiResult) { StatusCode = badRequestObjectResult.StatusCode };
                }
            }
            else if (context.Result is ContentResult contentResult)
            {
                var apiResult = new ApiResult(true, HttpStatusCode.OK, contentResult.Content);
                context.Result = new JsonResult(apiResult) { StatusCode = contentResult.StatusCode };
            }
            else if (context.Result is NotFoundResult notFoundResult)
            {
                var apiResult = new ApiResult(false, HttpStatusCode.NotFound);
                context.Result = new JsonResult(apiResult) { StatusCode = notFoundResult.StatusCode };
            }
            else if (context.Result is NotFoundObjectResult notFoundObjectResult)
            {
                var apiResult = new ApiResult<object>(false, HttpStatusCode.NotFound, notFoundObjectResult.Value);
                context.Result = new JsonResult(apiResult) { StatusCode = notFoundObjectResult.StatusCode };
            }
            else if (context.Result is ObjectResult objectResult)
            {
                if (objectResult.StatusCode == null && objectResult.Value is not ApiResult)
                {
                    var apiResult = new ApiResult<object>(true, HttpStatusCode.OK, objectResult.Value);
                    context.Result = new JsonResult(apiResult) { StatusCode = objectResult.StatusCode };
                }
                else
                {
                    if (objectResult.Value.GetType().GetProperty("Pagination")?.GetValue(objectResult.Value) is PaginationResult paginationResult
                        && paginationResult.PageSize.HasValue
                        && paginationResult.PageSize.Value >= 1)
                    {
                        var scheme = context.HttpContext.Request.Scheme;
                        var host = context.HttpContext.Request.Host.Value;
                        var pathBase = context.HttpContext.Request.PathBase.Value;
                        var path = context.HttpContext.Request.Path.Value;

                        string url = string.Concat(scheme, "://", host, pathBase, path);

                        var queryString = context.HttpContext.Request.QueryString.Value;
                        var queryParsed = QueryHelpers.ParseQuery(queryString).AsEnumerable();

                        var linksResults = new LinksResult();

                        if (paginationResult.PageNumber.HasValue)
                        {
                            linksResults.Next = paginationResult.PageNumber * paginationResult.PageSize < paginationResult.Total
                                                ? string.Concat(url, PaginationQueryString(queryString, nameof(PaginationOffsetQuery.PageNumber), (paginationResult.PageNumber + 1).ToString()))
                                                : null;


                            linksResults.Prev = paginationResult.PageNumber > 1
                                                ? string.Concat(url, PaginationQueryString(queryString, nameof(PaginationOffsetQuery.PageNumber), (paginationResult.PageNumber - 1).ToString()))
                                                : null;
                        }
                        else if (paginationResult.ContinuationSince != null)
                        {
                            linksResults.Next = paginationResult.PartialTotal > paginationResult.PageSize 
                                                ? string.Concat(url, PaginationQueryString(queryString, nameof(PaginationSinceQuery.Since), paginationResult.ContinuationSince.ToString()))
                                                : null;
                        }

                        if (linksResults.Next == null && linksResults.Prev == null)
                            linksResults = null;

                        objectResult.Value.GetType().GetProperty("Links").SetValue(objectResult.Value, linksResults);

                    }
                }
            }

            base.OnResultExecuting(context);
        }

        private string PaginationQueryString(string queryString, string parameter, string parameterValue)
        {
            var newQueryString = string.Empty;
            var queryParsed = QueryHelpers.ParseQuery(queryString).Where(x => !x.Key.Equals(parameter, StringComparison.InvariantCultureIgnoreCase));
            foreach (var item in queryParsed)
            {
                newQueryString = QueryHelpers.AddQueryString(newQueryString, item.Key, item.Value);
            }
            newQueryString = QueryHelpers.AddQueryString(newQueryString, parameter, parameterValue);

            return newQueryString;
        }
    }

}
