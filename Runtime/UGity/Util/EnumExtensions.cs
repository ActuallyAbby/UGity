using System.Collections.Generic;

namespace Octothorpe.UGity.Util
{
    public static class EnumExtensions
    {
        public static IEnumerable<E> Values<E>(this E enumType) where E : System.Enum
        {
            foreach(System.Enum value in System.Enum.GetValues(typeof(E)))
            {
                yield return (E) value;
            }
        }

        public static bool IsUniqueFlag<E>(this E flag) where E : System.Enum
        {
            int val = System.Convert.ToInt32(flag);
            return (val != 0) && (val & (val - 1)) == 0;
        }

        public static bool HasFlags<E>(this E flags, params E[] values) where E : System.Enum
        {
            int flagsVal = System.Convert.ToInt32(flags);

            foreach(E other in values)
            {
                int otherVal = System.Convert.ToInt32(other);
                if((flagsVal & otherVal) != otherVal) return false;
            }

            return true;
        }

        public static bool IsAny<E>(this E instance, params E[] others) where E : System.Enum
        {
            foreach(E other in others)
            {
                if(EqualityComparer<E>.Default.Equals(instance, other)) return true;
            }

            return false;
        }

        public static bool IsNone<E>(this E instance, params E[] others) where E : System.Enum
        {
            foreach(E other in others)
            {
                if(EqualityComparer<E>.Default.Equals(instance, other)) return false;
            }

            return true;
        }
    }
}
