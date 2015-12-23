using System.Collections.Generic;

namespace WindowPainless
{
    public class GenericStructuralEqualityComparer<TKey> : EqualityComparer<TKey>
    {
        public override bool Equals(TKey x, TKey y) =>
            System.Collections.StructuralComparisons.StructuralEqualityComparer.Equals(x, y);

        public override int GetHashCode(TKey obj) =>
            System.Collections.StructuralComparisons.StructuralEqualityComparer.GetHashCode(obj);
    }
}