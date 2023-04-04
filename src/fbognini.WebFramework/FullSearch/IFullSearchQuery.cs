using fbognini.Core.Data.Pagination;
using fbognini.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
