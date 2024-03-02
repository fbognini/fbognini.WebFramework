using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fbognini.WebFramework.Utilities
{
    public static class EnumerableAsyncExtensions
    {
        public static async Task<bool> AnyAsync<T>(
            this IEnumerable<T> source, Func<T, Task<bool>> func)
        {
            foreach (var element in source)
            {
                if (await func(element))
                {
                    return true;
                }
            }
            return false;
        }

        public static async Task<bool> AllAsync<T>(
            this IEnumerable<T> source, Func<T, Task<bool>> func)
        {
            foreach (var element in source)
            {
                if (!await func(element))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
