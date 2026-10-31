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
    /// MechJebModuleSmartASS in KodeKit, row for row with its WindowGUI. The module keeps the state and the Engage logic;
    /// the "other autopilot is using the attitude controller" check and the per-frame side effects are shared methods on the module.
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleSmartASS))]
    public class SmartASSView : KodeView
    {
        readonly MechJebModuleSmartASS ss;

        // the result of ss.OtherUsersControlAttitude() for this frame (it has side effects, so it runs once, in BeforeSync)
        bool otherUsers;

        public SmartASSView(MechJebModuleSmartASS ss) : base(ss) => this.ss = ss;

        protected override float MinWidth => 180;

        static readonly Color ActiveTint = new Color(0.5f, 1f, 0.5f);

        // the IMGUI window ran this before drawing
        protected override void BeforeSync()
        {
            otherUsers = ss.OtherUsersControlAttitude();
            if (!otherUsers) ss.ApplyPerFrameState();
        }

        static string TargetKey(Target t) => "#MechJeb_SmartASS_button" + (11 + (int)t);
        static string ModeKey(Mode m) => "#MechJeb_SmartASS_button" + (1 + (int)m);

        bool PatchedConics => ss.Vessel != null && ss.Vessel.patchedConicsUnlocked();

        // ---- private helpers (IMGUI's btNormal/btActive styles, bare checkbox, vertical panel) ----

        /// <summary>A button drawn with IMGUI's btActive look (green text and tint) while active() is true.</summary>
        static UIButton StateButton(KodeWindow w, UIObject parent, string text, Action click, Func<bool> active)
        {
            UIButton b = KodeWindow.Padded(KodeWindow.Button(parent, text, click, true)); // btNormal: padding 8
            var img = b.GetComponent<Image>();
            bool shown = active();
            void Tint(Theme th) => img.color = shown ? ActiveTint : th.Button.Tint;
            KodeWindow.Styled(b.gameObject, Tint);
            w.AddSync(() =>
            {
                bool a = active();
                if (a == shown) return;
                shown = a;
                Tint(KodeWindow.ThemeOf(b.gameObject));
            });
            w.ToneText(b.GetComponentInChildren<TMP_Text>(), () => active() ? KodeWindow.Tone.Good : KodeWindow.Tone.Normal, true);
            return b;
        }

        UIButton TargetButton(KodeWindow w, UIObject parent, Target bt, bool engage = true) =>
            StateButton(w, parent, TargetKey(bt), () =>
            {
                ss.target = bt;
                if (engage) ss.Engage();
            }, () => ss.target == bt);

        UIButton ModeButton(KodeWindow w, UIObject parent, Mode bt) =>
            StateButton(w, parent, ModeKey(bt), () => ss.mode = bt, () => ss.mode == bt);

        /// <summary>GUILayout.Toggle(value, "", NoExpandWidth): a checkbox with no label (a switch in the modern layout).</summary>
        static void CheckBox(KodeWindow w, UIObject parent, Func<bool> get, Action<bool> set)
        {
            if (KodeWindow.ModernLayout)
            {
                w.Switch(parent, get, set);
                return;
            }
            UIToggle t = parent.Add<UIToggle>();
            t.PreferredSize(16, 16).Finish();
            t.CheckMark.Scale(0.625f);
            Toggle toggle = t.GetComponent<Toggle>();
            toggle.SetIsOnWithoutNotify(get());
            set = w.Edits(set);
            toggle.onValueChanged.AddListener(v => set(v));
            var box = t.GetComponent<Image>();
            var check = (Image)toggle.graphic;
            KodeWindow.Styled(t.gameObject, th =>
            {
                th.Toggle.ApplySelectable(toggle, box);
                th.Check.ApplyImage(check);
                t.PreferredSize(th.FontSize + 2, th.FontSize + 2);
            });
            w.AddSync(() =>
            {
                bool v = get();
                if (toggle.isOn != v) toggle.SetIsOnWithoutNotify(v);
            });
        }

        // GUILayout.ExpandHeight(true) on every button of the row
        static Layout ButtonRow(UIObject parent) => KodeWindow.EqualHeights(KodeWindow.Row(parent));

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
            KodeWindow.Label(row, label);
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
                KodeWindow.Button(row, x.Text, () => apply(x.Value()));
            }
        }

        protected override void BuildContents(KodeWindow w)
        {
            if (KodeWindow.ModernLayout)
            {
                BuildModern(w);
                return;
            }

            UIObject r = w.Root;

            // another autopilot uses the attitude controller: just the red "AUTO"
            Layout autoPanel = KodeWindow.Panel(r);
            UIButton auto = KodeWindow.Padded(KodeWindow.Button(autoPanel, "#MechJeb_SmartASS_button57", () => { }, true)); // btAuto
            KodeWindow.ToneText(auto.GetComponentInChildren<TMP_Text>(), KodeWindow.Tone.Bad);
            w.Visible(autoPanel.gameObject, () => otherUsers);

            Layout main = KodeWindow.Panel(r);
            w.Visible(main.gameObject, () => !otherUsers);

            Layout top = ButtonRow(main);
            TargetButton(w, top, Target.OFF);
            TargetButton(w, top, Target.KILLROT);
            UIButton node = TargetButton(w, top, Target.NODE);
            w.Visible(node.gameObject, () => PatchedConics);
            UIButton noNode = KodeWindow.Button(top, "-", () => { }, true);
            w.Visible(noNode.gameObject, () => !PatchedConics);

            KodeWindow.Label(main, "#MechJeb_SmartASS_label1"); //"Mode:"
            Layout modes = ButtonRow(main);
            ModeButton(w, modes, Mode.ORBITAL);
            ModeButton(w, modes, Mode.SURFACE);
            ModeButton(w, modes, Mode.TARGET);
            ModeButton(w, modes, Mode.ADVANCED);

            // one panel per mode, each built the first time its mode is selected
            w.LazyPanel(main, () => ss.mode == Mode.ORBITAL, p => BuildOrbital(w, p));
            w.LazyPanel(main, () => ss.mode == Mode.SURFACE, p => BuildSurface(w, p));
            w.LazyPanel(main, () => ss.mode == Mode.TARGET, p => BuildTarget(w, p));
            w.LazyPanel(main, () => ss.mode == Mode.ADVANCED, p => BuildAdvanced(w, p));
            // Mode.AUTO shows nothing
        }

        void BuildOrbital(KodeWindow w, Layout p)
        {
            Layout a = ButtonRow(p);
            TargetButton(w, a, Target.PROGRADE);
            TargetButton(w, a, Target.NORMAL_PLUS);
            TargetButton(w, a, Target.RADIAL_PLUS);
            Layout b = ButtonRow(p);
            TargetButton(w, b, Target.RETROGRADE);
            TargetButton(w, b, Target.NORMAL_MINUS);
            TargetButton(w, b, Target.RADIAL_MINUS);
            ForceRoll(w, p);
        }

        void BuildSurface(KodeWindow w, Layout p)
        {

            Layout a = ButtonRow(p);
            TargetButton(w, a, Target.SURFACE_PROGRADE);
            TargetButton(w, a, Target.SURFACE_RETROGRADE);
            TargetButton(w, a, Target.SURFACE, false); // TargetButtonNoEngage
            Layout b = ButtonRow(p);
            TargetButton(w, b, Target.HORIZONTAL_PLUS);
            TargetButton(w, b, Target.HORIZONTAL_MINUS);
            TargetButton(w, b, Target.VERTICAL_PLUS);
            SurfaceAngles(w, p);
        }

        /// <summary>The surface targets' angle rows, smooth control and EXECUTE (both layouts).</summary>
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
            KodeWindow.Button(common, "#MechJeb_SmartASS_button58", () => ss.Engage(), ButtonRole.Primary, KodeWindow.ModernLayout); // "EXECUTE"
            w.Visible(common.gameObject, () => ss.ShowsSurfaceAngles);
        }

        void BuildTarget(KodeWindow w, Layout p)
        {

            Layout has = KodeWindow.Panel(p);
            Layout a = ButtonRow(has);
            TargetButton(w, a, Target.TARGET_PLUS);
            TargetButton(w, a, Target.RELATIVE_PLUS);
            TargetButton(w, a, Target.PARALLEL_PLUS);
            Layout b = ButtonRow(has);
            TargetButton(w, b, Target.TARGET_MINUS);
            TargetButton(w, b, Target.RELATIVE_MINUS);
            TargetButton(w, b, Target.PARALLEL_MINUS);
            ForceRoll(w, has);
            w.Visible(has.gameObject, TargetExists);

            UIText none = KodeWindow.Label(p, "#MechJeb_SmartASS_label2"); //"Please select a target"
            w.Visible(none.gameObject, () => !TargetExists());
        }

        bool TargetExists() => ss.Core.Target != null && ss.Core.Target.NormalTargetExists;

        // ---- modern layout: tiles with navball markers, the navball cross, a joined mode bar, an AUTO status bar ----

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

        void BuildModern(KodeWindow w)
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

        void BuildAdvanced(KodeWindow w, Layout p)
        {

            KodeWindow.Label(p, "#MechJeb_SmartASS_label3"); //"Reference:"
            new ComboBox(w, p, () => (int)ss.advReference, i => ss.advReference = (AttitudeReference)i, () => ReferenceTexts);

            KodeWindow.Label(p, "#MechJeb_SmartASS_label4"); //"Direction:"
            new ComboBox(w, p, () => (int)ss.advDirection, i => ss.advDirection = (Vector6.Direction)i, () => directionTexts);

            ForceRoll(w, p);

            KodeWindow.Button(p, "#MechJeb_SmartASS_button58", () => //"EXECUTE"
            {
                ss.target = Target.ADVANCED;
                ss.Engage();
            }, true);
        }
    }
}
