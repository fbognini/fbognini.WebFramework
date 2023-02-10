using fbognini.WebFramework.Validation;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace fbognini.WebFramework.Handlers.Problems
{
    public class DetailedValidationProblemDetails : ProblemDetails
    {
        public DetailedValidationProblemDetails(FluentValidation.Results.ValidationFailure failure)
        {
            Status = 400;
            Parameter = failure.PropertyName;

            if (failure.CustomState is DetailedValidationState state)
            {
                Title = failure.ErrorMessage;
                Code = failure.ErrorCode;
                var detail = state!.Detail;
                if (detail != null)
                {
                    detail = detail.Replace("{PropertyName}", failure.PropertyName);
                    detail = detail.Replace("{PropertyValue}", failure.AttemptedValue.ToString());
                }

                Detail = detail;
            }
            else
            {
                Title = "One or more validation errors occurred.";
                Code = "VALIDATION_ERROR";
                Detail = failure.ErrorMessage;
            }
        }

        [JsonPropertyOrder(100)]
        public string Code { get; set; }
        [JsonPropertyOrder(101)]
        public string Parameter { get; set; }

        [JsonIgnore]
        public new IDictionary<string, object?> Extensions => base.Extensions;
    }
}
