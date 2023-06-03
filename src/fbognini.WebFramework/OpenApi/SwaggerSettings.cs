namespace fbognini.WebFramework.OpenApi;

public class SwaggerSettings
{
    public bool Enable { get; set; }
    public string Title { get; set; }
    public string Version { get; set; }
    public string Description { get; set; }
    public string ContactName { get; set; }
    public string ContactEmail { get; set; }
    public string ContactUrl { get; set; }
    public bool License { get; set; }
    public string LicenseName { get; set; }
    public string LicenseUrl { get; set; }
    public bool UseFluentValidation { get; set; }
    public SwaggerAuthenticationSettings Authentication { get; set; }
}

public class SwaggerAuthenticationSettings
{
    public bool UseBearerAuthentication { get; set; }
    public bool UseApiKeyAuthentication { get; set; }
    public string? ApiKeyHeaderName { get; set; }

}