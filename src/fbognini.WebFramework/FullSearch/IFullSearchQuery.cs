using fbognini.Core.Domain.Query.Pagination;
using fbognini.Core.Domain.Query;
using System.Collections.Generic;

namespace fbognini.WebFramework.FullSearch
{
    public class FullSearch
    {
        public string? Search { get; set; }
        public PaginationOffsetQuery? Pagination { get; set; }
        public List<SortingQuery> Sortings { get; set; } = new();
    }

    public interface IFullSearchQuery
    {
        public FullSearch FullSearch { get; set; }
    }
}
