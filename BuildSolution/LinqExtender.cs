using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildSolution
{
    public static class LinqExtender 
    {

        /// <summary>
        /// run passed in function for each element in the list
        /// </summary>
        public static IEnumerable<T> RunFuncForEach<T>(this IEnumerable<T> @this, Action<T> func)
        {

            foreach(var item in @this)
            {
                func(item);
            }

            return @this;
        }

        public static T RemoveAndGet<T>(this IList<T> list, int index)
        {
            lock (list)
            {
                T value = list[index];
                list.RemoveAt(index);
                return value;
            }
        }
    }
}
