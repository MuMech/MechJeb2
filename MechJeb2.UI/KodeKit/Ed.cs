using System.Globalization;

namespace KodeKit
{
    /// <summary>Stand-in for MechJeb's EditableDouble: a value plus the text the user is editing.</summary>
    public class Ed
    {
        double val;
        string text;
        readonly double multiplier;

        public Ed(double v, double multiplier = 1)
        {
            this.multiplier = multiplier;
            Val = v;
        }

        public double Val
        {
            get => val;
            set
            {
                val = value;
                text = (val / multiplier).ToString(CultureInfo.InvariantCulture);
            }
        }

        public string Text
        {
            get => text;
            set
            {
                text = value;
                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                    val = d * multiplier;
            }
        }

        public static implicit operator double(Ed e) => e.Val;
    }
}
