using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using KodeKit;
using KodeUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MuMech.UI
{
    /// <summary>
    /// Builds the KodeKit widget of one InfoItem (what InfoItem.DrawItem draws in IMGUI), for custom info windows and
    /// any other window that shows registry items. Values are refreshed by the owner through Refresh() at its rate.
    /// </summary>
    public class InfoItemWidgets
    {
        readonly KodeWindow w;
        readonly List<Action> refresh = new List<Action>();

        /// <summary>Text colour override for value and label texts (custom windows' text colour); null = theme.</summary>
        public Func<Color?> TextColor;

        public InfoItemWidgets(KodeWindow w) => this.w = w;

        /// <summary>Refresh every value (call at the window's refresh rate).</summary>
        public void Refresh()
        {
            for (int i = 0; i < refresh.Count; i++) refresh[i]();
        }

        public void Add(UIObject parent, InfoItem item)
        {
            switch (item)
            {
                case ValueInfoItem v:
                    AddValue(parent, v);
                    break;
                case ToggleInfoItem t:
                    UnityEngine.UI.Toggle toggle = w.Toggle(parent, item.localizedName, () => t.Value, x => t.Value = x);
                    Tooltip(toggle.transform.parent.GetComponentInChildren<ToggleLabelClick>().gameObject, item);
                    break;
                case EditableInfoItem e:
                    AddEditable(parent, e);
                    break;
                case ActionInfoItem a:
                    UIButton b = KodeWindow.Button(parent, item.localizedName, a.Invoke, true);
                    Tooltip(b.gameObject, item);
                    break;
                case GeneralInfoItem g:
                    if (GeneralItemBuilders.TryBuild(w, parent, g, refresh))
                        break;
                    // not ported: the IMGUI method inside the window, refreshed like MechJebModuleCustomInfoWindow does
                    new ImguiBlock(parent, g.DrawItem);
                    refresh.Add(g.UpdateItem);
                    break;
                default:
                    KodeWindow.Label(parent, item.localizedName);
                    break;
            }
        }

        static void Tooltip(GameObject go, InfoItem item)
        {
            if (!string.IsNullOrEmpty(item.localizedTooltip)) Tooltips.Add(go, item.localizedTooltip);
        }

        void AddEditable(UIObject parent, EditableInfoItem e)
        {
            IEditable ed = e.Editable;
            if (ed == null) return;
            Layout row = KodeWindow.Row(parent);
            UIText label = KodeWindow.Label(row, e.localizedName, true);
            Tooltip(label.gameObject, e);
            Func<bool> valid = ed is EditableDoubleMult m ? (Func<bool>)(() => m.IsParsed) : null;
            EditFields.Field(w, row, () => ed.Text, s => ed.Text = s, e.width, valid);
            if (!string.IsNullOrEmpty(e.rightLabel)) KodeWindow.Label(row, e.rightLabel);
        }

        // ---- values ----

        void AddValue(UIObject parent, ValueInfoItem item)
        {
            Layout row = KodeWindow.Row(parent);
            UIText label = KodeWindow.Label(row, item.localizedName, true);
            Tooltip(label.gameObject, item);

            Func<CharText, int> native = NativeString(item);
            Func<double> number = native == null ? NumberGetter(item) : null;
            bool chars = native != null || number != null && Formattable(item, out _);
            // the cell's height is measured from its first text: native items start from MechJeb's string (Coordinates has two lines)
            string first = chars && native == null ? null : item.StringValue;
            UIText value = KodeWindow.LiveLabel(row, first ?? "0", false);
            value.tmpText.alignment = TextAlignmentOptions.Right;
            var size = value.transform.parent.GetComponent<LayoutElement>();
            if (item.Width > 0) size.preferredWidth = item.Width;

            ApplyColor(label.tmpText);
            ApplyColor(value.tmpText);

            int widest = 0;
            // a theme change re-sizes the cell from its template: re-apply the grown width at once, measured in the new theme
            w.Behaviour.AddRestyle(() =>
            {
                if (item.Width > 0 || widest == 0) return;
                float wanted = TextMeasure.Measure(new string('0', widest), Theme.Current).x + 8;
                if (wanted > size.preferredWidth) size.preferredWidth = wanted;
            });
            void Grow(int length)
            {
                // the value cell only grows, so changing numbers never re-lay out the window
                if (item.Width > 0 || length <= widest) return;
                widest = length;
                float wanted = TextMeasure.Measure(new string('0', length), Theme.Current).x + 8;
                if (wanted > size.preferredWidth) size.preferredWidth = wanted;
            }

            if (native != null)
            {
                var ct = new CharText(value.tmpText);
                refresh.Add(() =>
                {
                    Grow(native(ct.Clear()));
                    ct.Commit();
                });
            }
            else if (chars)
            {
                var ct = new CharText(value.tmpText);
                refresh.Add(() =>
                {
                    Format(ct.Clear(), item, number());
                    Grow(ct.Length);
                    ct.Commit();
                });
            }
            else
            {
                // strings, and numbers MechJeb formats with a .NET format CharText doesn't do: MechJeb's own string
                string shown = null;
                refresh.Add(() =>
                {
                    item.UpdateItem();
                    string s = item.StringValue;
                    if (s == shown) return;
                    shown = s;
                    Grow(s?.Length ?? 0);
                    value.tmpText.text = s;
                });
            }
        }

        void ApplyColor(TMP_Text t)
        {
            if (TextColor == null) return;
            Color? shown = null;
            // a restyle puts the theme colour back: re-apply the window's own colour at the next sync
            w.Behaviour.AddRestyle(() => shown = null);
            w.AddSync(() =>
            {
                Color? c = TextColor();
                if (c == shown) return;
                shown = c;
                t.color = c ?? Theme.Current.Text;
            });
        }

        /// <summary>The formats CharText reproduces exactly (see ValueInfoItem.GetStringValue).</summary>
        /// <summary>
        /// Items whose member returns a string MechJeb builds from numbers, written from those numbers without garbage. Same
        /// text as ValueInfoItem's ("{value} {units}", so a trailing space when there are no units). Returns the length of
        /// the widest line, for the cell width.
        /// </summary>
        static Func<CharText, int> NativeString(ValueInfoItem item)
        {
            if (!(item.Target is MechJebModuleInfoItems info)) return null;
            string units = item.Units;
            switch (item.id)
            {
                case "Value:InfoItems.GetCoordinateString":
                    // Coordinates.ToStringDMS(latitude, longitude, newline: true)
                    return ct =>
                    {
                        double lat = info.VesselState.Latitude, lon = MechJebLib.Utils.Statics.Clamp180(info.VesselState.Longitude);
                        ct.AppendDMS(lat).Append(lat > 0 ? " N" : " S");
                        int first = ct.Length;
                        ct.Append('\n').AppendDMS(lon).Append(lon > 0 ? " E" : " W").Append(' ').Append(units);
                        return Math.Max(first, ct.Length - first - 1);
                    };
                case "Value:InfoItems.StageDeltaVAtmosphereAndVac":
                    return ct =>
                    {
                        info.StageDeltaVPair(out double atm, out double vac);
                        ct.Append(atm, 0).Append(", ").Append(vac, 0).Append(' ').Append(units);
                        return ct.Length;
                    };
                case "Value:InfoItems.TotalDeltaVAtmosphereAndVac":
                    return ct =>
                    {
                        info.TotalDeltaVPair(out double atm, out double vac);
                        ct.Append(atm, 0).Append(", ").Append(vac, 0).Append(' ').Append(units);
                        return ct.Length;
                    };
            }
            return null;
        }

        static bool Formattable(ValueInfoItem item, out int decimals)
        {
            decimals = 0;
            string f = item.Format;
            if (f == ValueInfoItem.SI || f == ValueInfoItem.TIME || f == ValueInfoItem.ANGLE || f == ValueInfoItem.ANGLE_NS ||
                f == ValueInfoItem.ANGLE_EW)
                return true;
            if (f != null && f.Length == 2 && (f[0] == 'F' || f[0] == 'f') && char.IsDigit(f[1]))
            {
                decimals = f[1] - '0';
                return true;
            }
            // no format: only ints print the same way ("{i} {units}")
            return string.IsNullOrEmpty(f) && ReturnType(item.Member) == typeof(int);
        }

        static void Format(CharText ct, ValueInfoItem item, double d)
        {
            string f = item.Format;
            if (f == ValueInfoItem.TIME)
            {
                long day = KSPUtil.dateTimeFormatter.Day, year = KSPUtil.dateTimeFormatter.Year;
                ct.AppendDHMS(d, item.TimeDecimalPlaces, (int)(day / 3600), (int)(year / day));
            }
            else if (f == ValueInfoItem.ANGLE) ct.AppendDMS(d);
            else if (f == ValueInfoItem.ANGLE_NS) ct.AppendDMS(d).Append(d > 0 ? " N" : " S");
            else if (f == ValueInfoItem.ANGLE_EW) ct.AppendDMS(d).Append(d > 0 ? " E" : " W");
            else if (f == ValueInfoItem.SI) ct.AppendSI(d, item.SiSigFigs, item.SiMaxPrecision).Append(item.Units);
            else if (string.IsNullOrEmpty(f)) ct.AppendInt((long)d).Append(' ').Append(item.Units);
            else ct.Append(d, f[1] - '0').Append(' ').Append(item.Units);
        }

        static Type ReturnType(MemberInfo m) => m switch
        {
            FieldInfo f    => f.FieldType,
            PropertyInfo p => p.PropertyType,
            MethodInfo mi  => mi.ReturnType,
            _              => null
        };

        static readonly Dictionary<MemberInfo, Func<object, double>> numberGetters = new Dictionary<MemberInfo, Func<object, double>>();

        /// <summary>A non-boxing getter for members returning double, float or int (others: null, use the string).</summary>
        static Func<double> NumberGetter(ValueInfoItem item)
        {
            MemberInfo m = item.Member;
            Type t = ReturnType(m);
            if (m == null || item.Target == null || (t != typeof(double) && t != typeof(float) && t != typeof(int))) return null;
            if (m is MethodInfo mi && mi.GetParameters().Length != 0) return null;

            if (!numberGetters.TryGetValue(m, out Func<object, double> g))
            {
                Type owner = m.DeclaringType; // cache is per member, so cast to the declaring type
                var dm = new DynamicMethod("num_" + m.Name, typeof(double), new[] { typeof(object) }, owner, true);
                ILGenerator il = dm.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, owner);
                switch (m)
                {
                    case FieldInfo f:
                        il.Emit(OpCodes.Ldfld, f);
                        break;
                    case PropertyInfo p:
                        il.Emit(OpCodes.Callvirt, p.GetGetMethod(true));
                        break;
                    case MethodInfo method:
                        il.Emit(OpCodes.Callvirt, method);
                        break;
                }
                if (t != typeof(double)) il.Emit(OpCodes.Conv_R8);
                il.Emit(OpCodes.Ret);
                g = (Func<object, double>)dm.CreateDelegate(typeof(Func<object, double>));
                numberGetters[m] = g;
            }

            object target = item.Target;
            return () => g(target);
        }
    }
}
