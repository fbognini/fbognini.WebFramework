using fbognini.Core.Domain.Query;
using fbognini.Core.Domain.Query.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace fbognini.WebFramework.FullSearch
{
    public static class FullSearchUtils
    {
        public static QueryableCriteria<TEntity> LoadFullSearchQuery<TEntity>(this QueryableCriteria<TEntity> criteria, IFullSearchQuery query)
        {
            return criteria.LoadFullSearchQuery(query, new List<Expression<Func<TEntity, object>>>());
        }

        public static QueryableCriteria<TEntity> LoadFullSearchQuery<TEntity>(this QueryableCriteria<TEntity> criteria, IFullSearchQuery query, Expression<Func<TEntity, object>> searchField)
        {
            return criteria.LoadFullSearchQuery(query, new List<Expression<Func<TEntity, object>>>() { searchField });
        }

        public static QueryableCriteria<TEntity> LoadFullSearchQuery<TEntity>(this QueryableCriteria<TEntity> criteria, IFullSearchQuery query, List<Expression<Func<TEntity, object>>> searchFields)
        {
            ArgumentNullException.ThrowIfNull(query.FullSearch);

            foreach (var sorting in query.FullSearch.Sortings)
            {
                criteria.AddSorting(sorting.Key, sorting.Value);
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

            if (search.SortColumns.Count != search.SortDirections.Count)
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
                Sortings = new()
            };

            for (int i = 0; i < search.SortColumns.Count; i++)
            {
                var column = search.SortColumns.ElementAt(i);
                var direction = search.SortDirections.ElementAt(i).Equals("asc", StringComparison.OrdinalIgnoreCase) ? SortingDirection.ASCENDING : SortingDirection.DESCENDING;

                query.FullSearch.Sortings.Add(column, direction);
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
