using System;
using System.Collections.Generic;

namespace MuMech.UI
{
    /// <summary>
    /// Text rebuilt only when its key changes: bindings call the getter every frame, the string is made once per change.
    /// Keys compare with EqualityComparer, so a NaN key equals the last NaN instead of rebuilding every frame. Do not
    /// use an enum as the key: Unity's Mono compares enums through object Equals, which boxes every frame. Cast to int.
    /// </summary>
    internal static class Memo
    {
        public static Func<string> Of<T>(Func<T> key, Func<T, string> build)
        {
            EqualityComparer<T> cmp = EqualityComparer<T>.Default;
            T last = default;
            string text = null;
            return () =>
            {
                T k = key();
                if (text != null && cmp.Equals(k, last)) return text;
                last = k;
                text = build(k);
                return text;
            };
        }
    }
}
