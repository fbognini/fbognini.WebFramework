using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using fbognini.Core.Data.Pagination;

namespace fbognini.WebFramework.Api
{
    public class ApiResult
    {
        public bool IsSuccess { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string? Message { get; set; }

        public IDictionary<string, string[]>? Validations { get; set; }
        public object? AdditionalData { get; set; }

        public ApiResult(
            bool isSuccess,
            HttpStatusCode statusCode,
            string? message = null,
            IDictionary<string, string[]>? validations = null,
            object? additionalData = null)
        {
            IsSuccess = isSuccess;
            StatusCode = statusCode;
            Message = message;
            Validations = validations;
            AdditionalData = additionalData;
        }

        public static implicit operator ApiResult(OkResult result)
        {
            return new ApiResult(true, HttpStatusCode.OK);
        }

        public static implicit operator ApiResult(BadRequestResult result)
        {
            return new ApiResult(false, HttpStatusCode.BadRequest);
        }
    }

    public class ApiResult<TResponse> : ApiResult
        where TResponse : class
    {
        public TResponse? Response { get; set; }

        public ApiResult(
            bool isSuccess,
            HttpStatusCode statusCode,
            TResponse? response,
            string? message = null)
            : base(isSuccess, statusCode, message)
        {
            Response = response;
        }

        #region Implicit Operators

        public static implicit operator ApiResult<TResponse>(TResponse data)
        {
            return new ApiResult<TResponse>(true, HttpStatusCode.OK, data);
        }

        public static implicit operator ApiResult<TResponse>(OkResult result)
        {
            return new ApiResult<TResponse>(true, HttpStatusCode.OK, null);
        }

        public static implicit operator ApiResult<TResponse>(OkObjectResult result)
        {
            return new ApiResult<TResponse>(true, HttpStatusCode.OK, (TResponse)result.Value);
        }

        public static implicit operator ApiResult<TResponse>(BadRequestResult result)
        {
            return new ApiResult<TResponse>(false, HttpStatusCode.BadRequest, null);
        }

        public static implicit operator ApiResult<TResponse>(BadRequestObjectResult result)
        {
            var message = result.Value.ToString();
            if (result.Value is SerializableError errors)
            {
                var errorMessages = errors.SelectMany(p => (string[])p.Value).Distinct();
                message = string.Join(";", errorMessages);
            }
            return new ApiResult<TResponse>(false, HttpStatusCode.BadRequest, null, message);
        }

        public static implicit operator ApiResult<TResponse>(NotFoundResult result)
        {
            return new ApiResult<TResponse>(false, HttpStatusCode.NotFound, null);
        }

        public static implicit operator ApiResult<TResponse>(NotFoundObjectResult result)
        {
            return new ApiResult<TResponse>(false, HttpStatusCode.NotFound, (TResponse)result.Value);
        }

        public static implicit operator ApiResult<TResponse>(ContentResult result)
        {
            return new ApiResult<TResponse>(true, HttpStatusCode.OK, null, result.Content);
        }
        #endregion
    }

    public class ApiResult<TPagination, TResponse> : ApiResult
       where TPagination : PaginationResponse<TResponse>
       where TResponse : class
    {

        public IList<TResponse>? Response { get; set; }

        public PaginationResult? Pagination { get; set; }

        public LinksResult? Links { get; set; }

        public ApiResult(
            bool isSuccess,
            HttpStatusCode statusCode,
            PaginationResponse<TResponse> pagination,
            string? message = null)
            : base(isSuccess, statusCode, message)
        {
            Pagination = pagination.Pagination;
            Response = pagination.Items;
        }

        #region Implicit Operators
        public static implicit operator ApiResult<TPagination, TResponse>(PaginationResponse<TResponse> data)
        {
            return new ApiResult<TPagination, TResponse>(true, HttpStatusCode.OK, data);
        }
        #endregion
    }
}
