using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace fbognini.WebFramework.FullSearch
{
    public class FullSearchQueryParameters
    {
        [FromQuery(Name = "q")]
        public string? Search { get; set; }
        [FromQuery(Name = "length")]
        public int? PageSize { get; set; }
        [FromQuery(Name = "page")]
        public int? PageNumber { get; set; }
        [FromQuery(Name = "start")]
        public int? StartIndex { get; set; }
        [FromQuery(Name = "sort-by")]
        public List<string> SortColumns { get; set; } = new();
        [FromQuery(Name = "sort-dir")]
        public List<string> SortDirections { get; set; } = new();
    }
}
