using System;
using System.Collections.Generic;
using System.Text;

namespace CxAPI_Store
{
    public static class LinqExt
    {
        public static IEnumerable<T> LogLINQ<T>(this IEnumerable<T> enumerable, string logName, Func<T, string> printMethod)
        {
#if DEBUG
            int count = 0;
            foreach (var item in enumerable)
            {
                if (printMethod != null)
                {
                    Console.WriteLine($"{logName}|item {count} = {printMethod(item)}");
                }
                count++;
                yield return item;
            }
            Console.WriteLine($"{logName}|count = {count}");
#else
    return enumerable;
#endif
        }

    }
}
