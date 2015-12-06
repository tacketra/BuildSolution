﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// remove this comment, just testing github visual studio 2015 setup in win 10
namespace BuildSolution
{
    public static class LinqExtender 
    {

        public static double Median(this IEnumerable<double> source)
        {
            if (source.Count() == 0)
            {
                throw new InvalidOperationException("Cannot compute median for an empty set.");  // 
            }

            var sortedList = from number in source
                             orderby number
                             select number;

            int itemIndex = (int)sortedList.Count() / 2;

            if (sortedList.Count() % 2 == 0)
            {
                // Even number of items. 
                return (sortedList.ElementAt(itemIndex) + sortedList.ElementAt(itemIndex - 1)) / 2;
            }
            else
            {
                // Odd number of items. 
                return sortedList.ElementAt(itemIndex);
            }
        }

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

        /// <summary>
        /// Break a list of items into chunks of a specific size
        /// </summary>
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int chunksize)
        {
            while (source.Any())
            {
                yield return source.Take(chunksize);
                source = source.Skip(chunksize);
            }
        }
    }
}
