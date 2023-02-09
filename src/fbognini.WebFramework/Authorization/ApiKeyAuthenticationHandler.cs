using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace fbognini.WebFramework.Authorization
{
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
    {
        private string failReason;
        private readonly IOptions<ApiKeyAuthenticationSettings> settings;
        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<ApiKeyAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock, IOptions<ApiKeyAuthenticationSettings> settings) : base(options, logger, encoder, clock)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Options.ApiKeyName == null)
                throw new ArgumentNullException(nameof(Options.ApiKeyName));

            if (!Request.Headers.TryGetValue(Options.ApiKeyName, out var apiKeyHeaderValues))
            {
                failReason = $"Api Key was not provided. Please provide '{Options.ApiKeyName}' Header.";
                return AuthenticateResult.Fail(failReason);
                //throw new UnauthorizedAccessException($"Api Key was not provided. Please provide '{Options.ApiKeyName}' Header.");
            }

            var providedApiKey = apiKeyHeaderValues.FirstOrDefault();

            if (apiKeyHeaderValues.Count == 0 || string.IsNullOrWhiteSpace(providedApiKey))
            {
                failReason = $"Api Key was not provided. Please provide '{Options.ApiKeyName}' Header.";
                return AuthenticateResult.Fail(failReason);
                //throw new UnauthorizedAccessException($"Api Key was not provided. Please provide '{Options.ApiKeyName}' Header.");
            }

            string potentialApiKey = apiKeyHeaderValues.ToString();
            if (potentialApiKey != settings.Value.SecretToken)
            {
                failReason = $"Unauthorized client.";
                return AuthenticateResult.Fail(failReason);
            }

            var identity = new ClaimsIdentity(Options.AuthenticationType);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Options.Scheme);
            return AuthenticateResult.Success(ticket);
        }

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            if (failReason != null)
                throw new UnauthorizedAccessException(failReason);

            throw new UnauthorizedAccessException();
        }

        protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = 403;
            //Response.ContentType = ProblemDetailsContentType;
            //var problemDetails = new ForbiddenProblemDetails();

            //await Response.WriteAsync(JsonSerializer.Serialize(problemDetails, DefaultJsonSerializerOptions.Options));
            await Response.WriteAsync("HandleForbiddenAsync");
        }
    }
}
