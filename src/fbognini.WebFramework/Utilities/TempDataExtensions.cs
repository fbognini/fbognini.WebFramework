using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Text.Json;

namespace fbognini.WebFramework
{
    public static class TempDataExtensions
    {
        public static void Set<T>(this ITempDataDictionary tempData, string key, T value)
        {
            if (tempData.ContainsKey(key))
                tempData.Remove(key);

            tempData.Add(key, JsonSerializer.Serialize(value));
        }

        public static T Get<T>(this ITempDataDictionary tempData, string key)
        {
            if (!tempData.ContainsKey(key)) return default;

            var value = tempData[key] as string;

            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
    }
}
