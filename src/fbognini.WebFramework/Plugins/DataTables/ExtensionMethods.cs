using fbognini.Core.Data;
using fbognini.Core.Data.Pagination;
using System;
using System.Collections.Generic;

namespace fbognini.WebFramework.Plugins.DataTables
{
    //public static class ExtensionMethods
    //{
    //    public static void LoadDtParameters(
    //        this FormCriteria criteria,
    //        DtParameters dtParameters)
    //    {
    //        if (dtParameters == null || dtParameters.Columns == null)
    //            throw new ArgumentNullException(nameof(dtParameters));

    //        if (dtParameters.Order != null)
    //        {
    //            for(int i = 0; i < dtParameters.Order.Length; i++)
    //            {
    //                criteria.LoadSortingQuery(
    //                    new SortingQuery(
    //                        dtParameters.Columns[dtParameters.Order[i].Column].Data
    //                        , dtParameters.Order[i].Dir.ToString().ToLower() == "asc" ? SortingDirection.ASCENDING : SortingDirection.DESCENDING));

    //            }

    //            //orderCriteria = dtParameters.Columns[dtParameters.Order[0].Column].Data;
    //            //orderDirection = dtParameters.Order[0].Dir.ToString().ToLower() == "asc" ? SortingDirection.ASCENDING : SortingDirection.DESCENDING;
    //        }
    //        else
    //        {
    //            criteria.LoadSortingQuery(new SortingQuery(dtParameters.Columns[0].Data, SortingDirection.ASCENDING));
    //        } 


    //        var pagination = new PaginationOffsetQuery(dtParameters.Length, dtParameters.Start / dtParameters.Length + 1);
    //        criteria.LoadPaginationOffsetQuery(pagination);
    //    }
    //}

    public static class Utils
    {
        public static void LoadDtParameters(DtParameters dtParameters, out List<SortingQuery> sortings, out PaginationOffsetQuery pagination)
        {
            if (dtParameters == null || dtParameters.Columns == null)
                throw new ArgumentNullException(nameof(dtParameters));

            sortings = new List<SortingQuery>();

            if (dtParameters.Order != null)
            {
                for (int i = 0; i < dtParameters.Order.Length; i++)
                {
                    sortings.Add(
                        new SortingQuery(
                            dtParameters.Columns[dtParameters.Order[i].Column].Data
                            , dtParameters.Order[i].Dir.ToString().ToLower() == "asc" ? SortingDirection.ASCENDING : SortingDirection.DESCENDING));

                }
            }
            else
            {
                sortings.Add(new SortingQuery(dtParameters.Columns[0].Data, SortingDirection.ASCENDING));
            }

            pagination = new PaginationOffsetQuery(dtParameters.Length, dtParameters.Start / dtParameters.Length + 1);
        }
    }
}
