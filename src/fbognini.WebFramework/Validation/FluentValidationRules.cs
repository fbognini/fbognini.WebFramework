using FluentValidation;
using System;
using System.Linq;

namespace fbognini.WebFramework.Validation
{
    public static class FluentValidationRules
    {

        public static IRuleBuilderOptions<T, TProperty> In<T, TProperty>(this IRuleBuilder<T, TProperty> ruleBuilder, params TProperty[] validOptions)
        {
            string formatted;
            if (validOptions == null || validOptions.Length == 0)
            {
                throw new ArgumentException("At least one valid option is expected", nameof(validOptions));
            }
            else if (validOptions.Length == 1)
            {
                formatted = validOptions[0].ToStringWithNull();
            }
            else
            {
                // format like: option1, option2 or option3
                formatted = $"{string.Join(", ", validOptions.Select(vo => vo.ToStringWithNull()).ToArray(), 0, validOptions.Length - 1)} or {validOptions.Last()}";
            }

            return ruleBuilder
                .Must(validOptions.Contains)
                .WithMessage($"{{PropertyName}} must be one of these values: {formatted}");
        }

        public static IRuleBuilderOptions<T, TProperty> NotFound<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule)
        {
            return rule.WithDetailedError("RESOURCE_MISSING", "{PropertyName} not found", "No such {PropertyName}: '{PropertyValue}'");
        }

        public static IRuleBuilderOptions<T, TProperty> WithDetailedError<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, string code, string message, string? detail = null)
        {
            return rule.WithMessage(message).WithErrorCode(code)
                .WithState(x => new DetailedValidationState()
                {
                    Detail = detail
                });
        }

        private static string ToStringWithNull<TProperty>(this TProperty c)
        {
            if (c == null)
                return "null";

            return c.ToString()!;
        }
    }
}
