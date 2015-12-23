using System.Collections.Generic;
using System.Linq;

namespace WindowPainless.Extensions
{
    public static class IntegerExtensions
    {
        public static IEnumerable<int> To(this int start, int count)
            => Enumerable.Range(start, count).ToArray();
    }
}