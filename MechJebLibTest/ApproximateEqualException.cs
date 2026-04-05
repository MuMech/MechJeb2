using System.Globalization;
using Xunit.Sdk;

namespace MechJebLibTest
{
    public class ApproximateEqualException : XunitException
    {
        public ApproximateEqualException(string expected, string actual, double epsilon)
            : base(string.Format(
                CultureInfo.CurrentCulture,
                "NearlyEquals() Failure: Values are not approximately equal\n" +
                "Expected: {0}\n" +
                "Actual:   {1}\n" +
                "Epsilon:  {2:G}",
                expected, actual, epsilon))
        { }
    }
}
