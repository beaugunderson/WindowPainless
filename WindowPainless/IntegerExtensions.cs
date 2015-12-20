using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowPainless
{
    public static class IntegerExtensions
    {
        public static IEnumerable<int> To(this int start, int count)
            => Enumerable.Range(start, count).ToArray();
    }
}