using System.Threading.Tasks;

namespace fbognini.WebFramework.Logging
{
    public interface IRequestLoggingAdditionalParameterResolver
    {
        Task<object?> Resolve(string key);
    }
}
