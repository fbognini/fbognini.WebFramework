using Microsoft.AspNetCore.Routing;
using System.Text.RegularExpressions;

namespace fbognini.WebFramework.Transformers
{
    public partial class SlugifyParameterTransformer : IOutboundParameterTransformer
    {

#if NET7_0_OR_GREATER

        [GeneratedRegex("([a-z])([A-Z])")]
        private static partial Regex LettersRegex();

        public string? TransformOutbound(object? value)
        {
            // Slugify value
            return value != null ? LettersRegex().Replace(value.ToString()!, "$1-$2").ToLower() : null;
        }

#else

        public string TransformOutbound(object value)
        {
            // Slugify value
            return value == null ? null : Regex.Replace(value.ToString(), "([a-z])([A-Z])", "$1-$2").ToLower();
        }

#endif
    }
}
