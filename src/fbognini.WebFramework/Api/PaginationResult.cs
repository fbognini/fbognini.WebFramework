using fbognini.Core.Data.Pagination;
using Newtonsoft.Json;

namespace fbognini.WebFramework.Api
{
    public class PaginationResult
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? PageNumber { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? PageSize { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? Total { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ContinuationSince { get; set; }
        [JsonIgnore]
        internal int? PartialTotal { get; private set; }

        #region Implicit Operators
        public static implicit operator PaginationResult(Pagination data)
        {
            if (data == null)
                return null;

            return new PaginationResult()
            {
                PageNumber = data.PageNumber,
                PageSize = data.PageSize,
                Total = data.Total,
                ContinuationSince = data.ContinuationSince,
                PartialTotal = data.PartialTotal,
            };
        }
        #endregion
    }
}
