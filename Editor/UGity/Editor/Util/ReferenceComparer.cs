namespace Octothorpe.UGity.Editor.Util
{
    public class ReferenceComparer<T> : System.Collections.Generic.IEqualityComparer<T>
    {
        public bool Equals(T x, T y) => object.ReferenceEquals(x, y);
        public int GetHashCode(T obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
