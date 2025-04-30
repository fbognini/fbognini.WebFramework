using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

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
        public string[] SortColumns { get; set; } = Array.Empty<string>();
        [FromQuery(Name = "sort-dir")]
        public string[] SortDirections { get; set; } = Array.Empty<string>();

        public static ValueTask<FullSearchQueryParameters?> BindAsync(HttpContext context, ParameterInfo parameter)
        {
            var query = context.Request.Query;
            return ValueTask.FromResult<FullSearchQueryParameters?>(new FullSearchQueryParameters
            {
                Search = query["q"],
                PageSize = int.TryParse(query["length"], out var size) ? size : null,
                PageNumber = int.TryParse(query["page"], out var number) ? number : null,
                StartIndex = int.TryParse(query["start"], out var start) ? start : null,
                SortColumns = query["sort-by"].Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!).ToArray(),
                SortDirections = query["sort-dir"].Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!).ToArray()
            });
        }
    }
}
