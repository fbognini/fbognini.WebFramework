using FastIDs.TypeId.Serialization.SystemTextJson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace fbognini.WebFramework.JsonConverters
{
    public static class JsonSerializerHelper
    {
        public static readonly JsonSerializerOptions WebOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web).ConfigureForTypeId();
        public static readonly JsonSerializerOptions LogOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            }
            .ConfigureForTypeId();
    }
}
