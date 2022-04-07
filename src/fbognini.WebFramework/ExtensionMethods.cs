using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Newtonsoft.Json;

namespace fbognini.WebFramework
{
    public static class TempDataExtensions
    {
        public static void Set<T>(this ITempDataDictionary tempData, string key, T value)
        {
            string json = JsonConvert.SerializeObject(value);

            if (tempData.ContainsKey(key))
                tempData.Remove(key);

            tempData.Add(key, json);
        }

        public static T Get<T>(this ITempDataDictionary tempData, string key)
        {
            if (!tempData.ContainsKey(key)) return default(T);

            var value = tempData[key] as string;

            return value == null ? default(T) : JsonConvert.DeserializeObject<T>(value);
        }
    }
}
