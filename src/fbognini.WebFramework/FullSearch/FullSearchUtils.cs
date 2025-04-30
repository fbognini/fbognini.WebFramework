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
            => criteria.LoadFullSearch(query.FullSearch);

        public static QueryableCriteria<TEntity> LoadFullSearchQuery<TEntity>(this QueryableCriteria<TEntity> criteria, IFullSearchQuery query, Expression<Func<TEntity, object>> searchField)
            => criteria.LoadFullSearch(query.FullSearch, searchField);

        public static QueryableCriteria<TEntity> LoadFullSearchQuery<TEntity>(this QueryableCriteria<TEntity> criteria, IFullSearchQuery query, List<Expression<Func<TEntity, object>>> searchFields)
            => criteria.LoadFullSearch(query.FullSearch, searchFields);

        public static QueryableCriteria<TEntity> LoadFullSearch<TEntity>(this QueryableCriteria<TEntity> criteria, FullSearch fullSearch)
            => criteria.LoadFullSearch(fullSearch, new List<Expression<Func<TEntity, object>>>());

        public static QueryableCriteria<TEntity> LoadFullSearch<TEntity>(this QueryableCriteria<TEntity> criteria, FullSearch fullSearch, Expression<Func<TEntity, object>> searchField)
            => criteria.LoadFullSearch(fullSearch, new List<Expression<Func<TEntity, object>>>() { searchField });

        public static QueryableCriteria<TEntity> LoadFullSearch<TEntity>(this QueryableCriteria<TEntity> criteria, FullSearch fullSearch, List<Expression<Func<TEntity, object>>> searchFields)
        {
            ArgumentNullException.ThrowIfNull(fullSearch);

            foreach (var sorting in fullSearch.Sortings)
            {
                criteria.AddSorting(sorting.Key, sorting.Value);
            }
            if (fullSearch.Pagination != null)
            {
                criteria.LoadPaginationOffsetQuery(fullSearch.Pagination);
            }
            if (!string.IsNullOrEmpty(fullSearch.Search))
            {
                criteria.Search.Keyword = fullSearch.Search;
                criteria.Search.Fields.AddRange(searchFields);
            }

            return criteria;
        }

        public static T LoadFullSearchParameters<T>(this T query, FullSearchQueryParameters search)
            where T: IFullSearchQuery
        {
            ArgumentNullException.ThrowIfNull(query);

            query.FullSearch = search.ToFullSearch();

            return query;
        }

        public static FullSearch ToFullSearch(this FullSearchQueryParameters search)
        {
            ArgumentNullException.ThrowIfNull(search);

            if (search.SortColumns.Length != search.SortDirections.Length)
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

            var fullSearch = new FullSearch
            {
                Search = search.Search,
                Sortings = new()
            };

            for (int i = 0; i < search.SortColumns.Length; i++)
            {
                var column = search.SortColumns.ElementAt(i);
                var direction = search.SortDirections.ElementAt(i).Equals("asc", StringComparison.OrdinalIgnoreCase) ? SortingDirection.ASCENDING : SortingDirection.DESCENDING;

                fullSearch.Sortings.Add(column, direction);
            }

            if (search.PageSize.HasValue)
            {
                if (search.StartIndex.HasValue)
                {
                    fullSearch.Pagination = new PaginationOffsetQuery(search.PageSize.Value, search.StartIndex.Value / search.PageSize.Value + 1);
                }
                else
                {
                    fullSearch.Pagination = new PaginationOffsetQuery(search.PageSize.Value, search.PageNumber!.Value);
                }
            }

            return fullSearch;
        }

    }
}
