using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;  
using ValidationException = fbognini.FluentValidation.Exceptions.ValidationException;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace fbognini.WebFramework
{
    public static class CustomValidatorInterceptorExtensions
    {
        public static IServiceCollection UseCustomValidatorInterceptor(this IServiceCollection services)
        {
            return services.AddTransient<IValidatorInterceptor, CustomValidatorInterceptor>();
        }
    }

    public class CustomValidatorInterceptor : IValidatorInterceptor
    {
        public IValidationContext BeforeMvcValidation(ControllerContext controllerContext, IValidationContext validationContext)
        {
            return validationContext;
        }

        public ValidationResult AfterMvcValidation(ControllerContext controllerContext, IValidationContext validationContext, ValidationResult result)
        {
            var failures = result.Errors
                .Where(f => f != null)
                .ToList();

            if (failures.Count != 0)
            {
                throw new ValidationException(failures);
            }


            return result;
        }

        public IValidationContext BeforeAspNetValidation(ActionContext actionContext, IValidationContext commonContext)
        {
            return commonContext;
        }

        public ValidationResult AfterAspNetValidation(ActionContext actionContext, IValidationContext validationContext, ValidationResult result)
        {
            var failures = result.Errors
                .Where(f => f != null)
                .ToList();

            if (failures.Count != 0)
            {
                throw new ValidationException(failures);
            }


            return result;
        }
    }
}
