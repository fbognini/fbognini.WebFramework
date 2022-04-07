using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;

namespace fbognini.WebFramework.Plugins.CustomSelectList
{
    public static class ExtensionMethods
    {
        public static IEnumerable<SelectListItemDto> ToSelectListDto<TSource, TKey1, TKey2>(this IEnumerable<TSource> list, Expression<Func<TSource, TKey1>> valueSelector, Expression<Func<TSource, TKey2>> textSelector)
        {
            var listItems = new List<SelectListItemDto>();
            foreach (var item in list)
            {
                var newListItem = new SelectListItemDto(Eval(item, ((MemberExpression)valueSelector.Body).Member.Name), Eval(item, ((MemberExpression)textSelector.Body).Member.Name));
                listItems.Add(newListItem);
            }

            return listItems;
        }

        public static SelectList ToSelectList(this IEnumerable<SelectListItemDto> list)
        {
            return new SelectList(list, "Value", "Text");
        }

        private static string Eval(object container, string expression)
        {
            var value = container;
            if (!string.IsNullOrEmpty(expression))
            {
                value = container.GetType().GetProperty(expression).GetValue(container, null);
            }

            return Convert.ToString(value, CultureInfo.CurrentCulture);
        }


    }
}
