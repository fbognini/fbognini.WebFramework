using fbognini.Core.Data;
using fbognini.Core.Data.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace fbognini.WebFramework.FullSearch
{
    public static class FullSearchUtils
    {
        public static SelectCriteria<TEntity> LoadFullSearchQuery<TEntity>(this SelectCriteria<TEntity> criteria, IFullSearchQuery query)
        {
            return criteria.LoadFullSearchQuery(query, new List<Expression<Func<TEntity, object>>>());
        }

        public static SelectCriteria<TEntity> LoadFullSearchQuery<TEntity>(this SelectCriteria<TEntity> criteria, IFullSearchQuery query, Expression<Func<TEntity, object>> searchField)
        {
            return criteria.LoadFullSearchQuery(query, new List<Expression<Func<TEntity, object>>>() { searchField });
        }

        public static SelectCriteria<TEntity> LoadFullSearchQuery<TEntity>(this SelectCriteria<TEntity> criteria, IFullSearchQuery query, List<Expression<Func<TEntity, object>>> searchFields)
        {
            ArgumentNullException.ThrowIfNull(query.FullSearch);

            foreach (var sorting in query.FullSearch.Sortings)
            {
                criteria.LoadSortingQuery(sorting);
            }
            if (query.FullSearch.Pagination != null)
            {
                criteria.LoadPaginationOffsetQuery(query.FullSearch.Pagination);
            }
            if (!string.IsNullOrEmpty(query.FullSearch.Search))
            {
                criteria.Search.Keyword = query.FullSearch.Search;
                criteria.Search.Fields.AddRange(searchFields);
            }

            return criteria;
        }

        public static T LoadFullSearchParameters<T>(this T query, FullSearchQueryParameters search)
            where T: IFullSearchQuery
        {
            ArgumentNullException.ThrowIfNull(query);
            ArgumentNullException.ThrowIfNull(search);

            if (search.OrderBys.Count != search.OrderDirections.Count)
            {
                throw new ArgumentException("Sortings are not valid");
            }

            if (search.PageSize.HasValue)
            {
                if (search.PageSize <= 0)
                {
                    throw new ArgumentException("Pagination size must be greater than 0 (empty for non paginated result)");
                }

                if (!search.StartIndex.HasValue && !search.PageNumber.HasValue || search.StartIndex.HasValue && search.PageNumber.HasValue)
                {
                    throw new ArgumentException("You should provide one between pagination index and page");
                }

                if (search.StartIndex.HasValue && search.StartIndex < 0)
                {
                    throw new ArgumentException("Pagination start must be greater or equal than 0");
                }

                if (search.PageNumber.HasValue && search.PageNumber <= 0)
                {
                    throw new ArgumentException("Pagination page must be greater than 0");
                }
            }

            query.FullSearch = new FullSearch
            {
                Search = search.Search,
                Sortings = new List<SortingQuery>()
            };

            for (int i = 0; i < search.OrderBys.Count; i++)
            {
                query.FullSearch.Sortings.Add(new SortingQuery(search.OrderBys.ElementAt(i), search.OrderDirections.ElementAt(i).Equals("asc", StringComparison.OrdinalIgnoreCase) ? SortingDirection.ASCENDING : SortingDirection.DESCENDING));
            }

            if (search.PageSize.HasValue)
            {
                if (search.StartIndex.HasValue)
                {
                    query.FullSearch.Pagination = new PaginationOffsetQuery(search.PageSize.Value, search.StartIndex.Value / search.PageSize.Value + 1);
                }
                else
                {
                    query.FullSearch.Pagination = new PaginationOffsetQuery(search.PageSize.Value, search.PageNumber.Value);
                }
            }

            return query;
        }
    }
}
