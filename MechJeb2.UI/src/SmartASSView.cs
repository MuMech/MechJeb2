using System;
using KodeKit;
using KodeUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MuMech.MechJebModuleSmartASS;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleSmartASSWindow in KodeKit. MechJebModuleSmartASS keeps the state and the Engage logic;
    /// the "other autopilot is using the attitude controller" check and the per-frame side effects are shared methods on the module.
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleSmartASSWindow))]
    public class SmartASSView : KodeView
    {
        readonly MechJebModuleSmartASS ss;

        // another module drives the attitude controller, as the module found on its last tick
        bool otherUsers;

        public SmartASSView(MechJebModuleSmartASSWindow window) : base(window) => ss = window.Core.SmartASS;

        protected override float MinWidth => 180;

        // the IMGUI window ran this before drawing
        protected override void BeforeSync()
        {
            otherUsers = ss.OtherUsersControlAttitude;
        }

        static string TargetKey(Target t) => "#MechJeb_SmartASS_button" + (11 + (int)t);
        static string ModeKey(Mode m) => "#MechJeb_SmartASS_button" + (1 + (int)m);

        bool PatchedConics => ss.Vessel != null && ss.Vessel.patchedConicsUnlocked();

        // ---- private helpers ----

        /// <summary>GUILayout.Toggle(value, "", NoExpandWidth): a switch with no label.</summary>
        static void CheckBox(KodeWindow w, UIObject parent, Func<bool> get, Action<bool> set)
        {
            w.Switch(parent, get, set);
        }

        // ---- rows ----

        /// <summary>ForceRoll(): [x] Force Roll : [rol] °</summary>
        void ForceRoll(KodeWindow w, UIObject parent)
        {
            Layout row = KodeWindow.Row(parent);
            void Set(bool v)
            {
                if (v == ss.forceRol) return;
                ss.forceRol = v;
                ss.Engage();
            }
            w.Toggle(row, "#MechJeb_SmartASS_checkbox3", () => ss.forceRol, Set);
            EditUi.Field(w, row, () => ss.rol, 30);
            KodeWindow.Label(row, "°");
        }

        class Extra
        {
            public string Text;
            public Func<double> Value;
            public Extra(string text, Func<double> value)
            {
                Text = text;
                Value = value;
            }
        }

        /// <summary>
        /// [force] LABEL [value] ° [--] [-] [+] [++] extras...
        /// Every button sets the value and re-engages without a PID reset, like the IMGUI "changed" flag.
        /// </summary>
        void AngleRow(KodeWindow w, UIObject parent, string label, Func<EditableDouble> get, Action<EditableDouble> set,
            Func<bool> force, Action<bool> setForce, params Extra[] extras)
        {
            Layout row = KodeWindow.Row(parent);
            CheckBox(w, row, force, setForce);
            // the label and the extra buttons get fixed widths, so the fields and buttons line up from row to row
            FixedWidth(KodeWindow.Label(row, label), 0, "HDG", "PIT", "ROL", "YAW");
            EditUi.Field(w, row, get, 37);
            KodeWindow.Label(row, "°");

            // change by 5 if the mod_key is held down, else by 1
            Func<double> val = () => GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1;
            Action<double> apply = v =>
            {
                set(v);
                ss.Engage(false);
            };
            KodeWindow.Button(row, "--", () => apply(get() - LARGE_INCREMENT));
            KodeWindow.Button(row, "-", () => apply(get() - val()));
            KodeWindow.Button(row, "+", () => apply(get() + val()));
            KodeWindow.Button(row, "++", () => apply(get() + LARGE_INCREMENT));
            foreach (Extra e in extras)
            {
                Extra x = e;
                FixedWidth(KodeWindow.Button(row, x.Text, () => apply(x.Value())), 12, "180", "CUR"); // 12: the button's padding
            }
        }

        /// <summary>Make a widget as wide as the widest of <paramref name="texts"/> plus <paramref name="padding"/>, measured in the window's theme.</summary>
        static void FixedWidth(Component c, float padding, params string[] texts)
        {
            var le = c.GetComponent<LayoutElement>() ?? c.gameObject.AddComponent<LayoutElement>();
            KodeWindow.Styled(c.gameObject, th =>
            {
                float width = 0;
                foreach (string t in texts) width = Mathf.Max(width, TextMeasure.Measure(t, th).x);
                le.minWidth = le.preferredWidth = Mathf.Ceil(width + padding);
            });
        }

        /// <summary>The surface targets' angle rows, smooth control and EXECUTE.</summary>
        void SurfaceAngles(KodeWindow w, UIObject p)
        {
            // target == SURFACE: heading, pitch, roll
            Layout hpr = KodeWindow.Panel(p);
            AngleRow(w, hpr, "HDG", () => ss.srfHdg, v => ss.srfHdg = v, () => ss.forceYaw, v => ss.forceYaw = v,
                new Extra("0", () => 0), new Extra("90", () => 90));
            AngleRow(w, hpr, "PIT", () => ss.srfPit, v => ss.srfPit = v, () => ss.forcePitch, v => ss.forcePitch = v,
                new Extra("0", () => 0), new Extra("90", () => 90));
            AngleRow(w, hpr, "ROL", () => ss.srfRol, v => ss.srfRol = v, () => ss.forceRol, v => ss.forceRol = v,
                new Extra("0", () => 0), new Extra("180", () => 180));
            w.Visible(hpr.gameObject, () => ss.target == Target.SURFACE);

            // target == SURFACE_PROGRADE / SURFACE_RETROGRADE: roll, pitch, yaw relative to the surface velocity
            Layout vel = KodeWindow.Panel(p);
            AngleRow(w, vel, "ROL", () => ss.srfVelRol, v => ss.srfVelRol = v, () => ss.forceRol, v => ss.forceRol = v,
                new Extra("CUR", () => ss.VesselState.Roll), new Extra("0", () => 0));
            AngleRow(w, vel, "PIT", () => ss.srfVelPit, v => ss.srfVelPit = v, () => ss.forcePitch, v => ss.forcePitch = v,
                new Extra("CUR", () => ss.VesselState.AoA), new Extra("0", () => 0));
            AngleRow(w, vel, "YAW", () => ss.srfVelYaw, v => ss.srfVelYaw = v, () => ss.forceYaw, v => ss.forceYaw = v,
                new Extra("CUR", () => -ss.VesselState.AoS), new Extra("0", () => 0));
            w.Visible(vel.gameObject, () => ss.target == Target.SURFACE_PROGRADE || ss.target == Target.SURFACE_RETROGRADE);

            // DisplaySmoothControlUI + EXECUTE, common to both
            Layout common = KodeWindow.Panel(p);
            Layout smooth = KodeWindow.Row(common);
            w.Toggle(smooth, "Smooth Control:", () => ss.smoothControl, v => ss.smoothControl = v);
            EditUi.Field(w, smooth, () => ss.degreesPerSecond, 40);
            KodeWindow.Label(smooth, "°/s");
            KodeWindow.Button(common, "#MechJeb_SmartASS_button58", () => ss.Engage(), ButtonRole.Primary, true); // "EXECUTE"
            w.Visible(common.gameObject, () => ss.ShowsSurfaceAngles);
        }

        bool TargetExists() => ss.Core.Target != null && ss.Core.Target.NormalTargetExists;

        // ---- Tiles with navball markers, the navball cross, a joined mode bar, an AUTO status bar ----

        /// <summary>A target tile, selected while it is SmartASS's target; with its navball marker when it has one.</summary>
        UIButton Tile(KodeWindow w, UIObject parent, Target bt, Sprites.Marker? marker = null, bool engage = true)
        {
            UIButton b = KodeWindow.Button(parent, TargetKey(bt), () =>
            {
                ss.target = bt;
                if (engage) ss.Engage();
            }, true);
            b.Content.Padding(5, 5, 5, 5);
            if (marker.HasValue)
            {
                Sprites.Marker m = marker.Value;
                b.Content.Spacing(4);
                Image icon = KodeWindow.AddIcon(b, Sprites.NavballMarker(m), true);
                icon.name = "Marker"; // keeps its navball colour when the tile is selected
                var le = icon.GetComponent<LayoutElement>();
                KodeWindow.Styled(icon.gameObject, th =>
                {
                    icon.color = Sprites.MarkerColor(m);
                    le.minWidth = le.preferredWidth = le.minHeight = le.preferredHeight = th.FontSize + 4;
                });
            }
            w.BindRole(b, () => ss.target == bt ? ButtonRole.Selected : ButtonRole.Normal);
            return b;
        }

        static readonly string[] modeKeys = { ModeKey(Mode.ORBITAL), ModeKey(Mode.SURFACE), ModeKey(Mode.TARGET), ModeKey(Mode.ADVANCED) };

        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;
            Layout body = KodeWindow.Panel(r);

            Layout top = KodeWindow.UniformGrid(body, 3);
            Tile(w, top, Target.OFF);
            Tile(w, top, Target.KILLROT);
            UIButton node = Tile(w, top, Target.NODE);
            w.Visible(node.gameObject, () => PatchedConics);

            KodeWindow.Header(body, "#MechJeb_SmartASS_label1"); //"Mode:"
            // navigation: the mode only changes which targets the window shows
            w.Segmented(body, modeKeys, () => (int)ss.mode, i => ss.mode = (Mode)i, ChoiceStyle.Tabs);

            // orbital: placed as on the navball, normal up, radial left and right, prograde in the centre
            w.LazyPanel(body, () => ss.mode == Mode.ORBITAL, obt =>
            {
                KodeWindow.CompassGrid c = KodeWindow.Compass(obt);
                Tile(w, c[Dir.N], Target.NORMAL_PLUS, Sprites.Marker.Normal);
                Tile(w, c[Dir.W], Target.RADIAL_MINUS, Sprites.Marker.RadialIn);
                Tile(w, c[Dir.C], Target.PROGRADE, Sprites.Marker.Prograde);
                Tile(w, c[Dir.E], Target.RADIAL_PLUS, Sprites.Marker.RadialOut);
                Tile(w, c[Dir.S], Target.NORMAL_MINUS, Sprites.Marker.AntiNormal);
                Tile(w, c[Dir.SE], Target.RETROGRADE, Sprites.Marker.Retrograde);
                ForceRoll(w, obt);
            });

            w.LazyPanel(body, () => ss.mode == Mode.SURFACE, srf =>
            {
                Layout sg = KodeWindow.UniformGrid(srf, 3);
                Tile(w, sg, Target.SURFACE_PROGRADE, Sprites.Marker.Prograde);
                Tile(w, sg, Target.SURFACE_RETROGRADE, Sprites.Marker.Retrograde);
                Tile(w, sg, Target.SURFACE, null, false); // TargetButtonNoEngage
                Tile(w, sg, Target.HORIZONTAL_PLUS);
                Tile(w, sg, Target.HORIZONTAL_MINUS);
                Tile(w, sg, Target.VERTICAL_PLUS);
                SurfaceAngles(w, srf);
            });

            w.LazyPanel(body, () => ss.mode == Mode.TARGET, tgt =>
            {
                Layout has = KodeWindow.Panel(tgt);
                Layout tg = KodeWindow.UniformGrid(has, 3);
                Tile(w, tg, Target.TARGET_PLUS);
                Tile(w, tg, Target.RELATIVE_PLUS);
                Tile(w, tg, Target.PARALLEL_PLUS);
                Tile(w, tg, Target.TARGET_MINUS);
                Tile(w, tg, Target.RELATIVE_MINUS);
                Tile(w, tg, Target.PARALLEL_MINUS);
                ForceRoll(w, has);
                w.Visible(has.gameObject, TargetExists);
                UIText none = KodeWindow.Label(tgt, "#MechJeb_SmartASS_label2"); //"Please select a target"
                w.Visible(none.gameObject, () => !TargetExists());
            });

            w.LazyPanel(body, () => ss.mode == Mode.ADVANCED, adv =>
            {
                KodeWindow.Header(adv, "#MechJeb_SmartASS_label3"); //"Reference:"
                new ComboBox(w, adv, () => (int)ss.advReference, i => ss.advReference = (AttitudeReference)i, () => ReferenceTexts);
                KodeWindow.Header(adv, "#MechJeb_SmartASS_label4"); //"Direction:"
                new ComboBox(w, adv, () => (int)ss.advDirection, i => ss.advDirection = (Vector6.Direction)i, () => directionTexts);
                ForceRoll(w, adv);
                KodeWindow.Button(adv, "#MechJeb_SmartASS_button58", () => //"EXECUTE"
                {
                    ss.target = Target.ADVANCED;
                    ss.Engage();
                }, ButtonRole.Primary, true);
            });

            // another autopilot steers: the body stays in view, greyed, and the bar says who
            GuidanceUi.AutoBar(w, r, body.gameObject, "#MechJeb_SmartASS_button57", () => otherUsers, //"AUTO"
                () => GuidanceUi.OtherUser(ss.Core.Attitude.Users, ss));
        }

    }
}
