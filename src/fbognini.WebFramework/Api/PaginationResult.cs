using System.Text.Json.Serialization;

namespace fbognini.WebFramework.Api
{
    public class PaginationResult
    {
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
        public int? Total { get; set; }
        public string? ContinuationSince { get; set; }
        [JsonIgnore]
        internal int? PartialTotal { get; private set; }

        #region Implicit Operators
        public static implicit operator PaginationResult(fbognini.Core.Data.Pagination.PaginationResult data)
        {
            if (data == null)
            {
                return null;
            }

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
