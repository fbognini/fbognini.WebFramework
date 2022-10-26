namespace fbognini.WebFramework.OpenApi;

public class TenantIdHeaderAttribute : SwaggerHeaderAttribute
{
    public TenantIdHeaderAttribute(string name)
       : base(
           name,
           "Input your tenant Id to access this API",
           string.Empty,
           true)
    {
    }
}
