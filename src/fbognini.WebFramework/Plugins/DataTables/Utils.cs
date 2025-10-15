using fbognini.Core.Domain.Query;
using fbognini.Core.Domain.Query.Pagination;
using System;
using System.Collections.Generic;

namespace fbognini.WebFramework.Plugins.DataTables
{
    public static class Utils
    {
        public static void LoadDtParameters(DtParameters dtParameters, out Dictionary<string, SortingDirection> sortings, out PaginationOffsetQuery pagination)
        {
            if (dtParameters == null || dtParameters.Columns == null)
                throw new ArgumentNullException(nameof(dtParameters));

            sortings = new Dictionary<string, SortingDirection>();

            if (dtParameters.Order != null)
            {
                for (int i = 0; i < dtParameters.Order.Length; i++)
                {
                    sortings.Add(dtParameters.Columns[dtParameters.Order[i].Column].Data, dtParameters.Order[i].Dir == DtOrderDir.ASC
                                ? SortingDirection.ASCENDING
                                : SortingDirection.DESCENDING);
                }
            }
            else
            {
                sortings.Add(dtParameters.Columns[0].Data, SortingDirection.ASCENDING);
            }

            pagination = new PaginationOffsetQuery(dtParameters.Length, dtParameters.Start / dtParameters.Length + 1);
        }
    }
}
