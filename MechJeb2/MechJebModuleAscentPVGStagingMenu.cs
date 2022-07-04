using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleAscentPVGStagingMenu : DisplayModule
    {
        public MechJebModuleAscentPVGStagingMenu(MechJebCore core) : base(core)
        {
        }

        public override void OnModuleEnabled()
        {
            base.OnModuleEnabled();
            _optimizeStage = 0;
            for (int i = 0; i < _inertial.Count; i++)
                _inertial[i] = false;
        }

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble MinDeltaV = new EditableDouble(40);

        [Persistent(pass = (int)Pass.Type)] public bool ExtendIfRequired = true;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble MaxCoast = new EditableDouble(450);

        [Persistent(pass = (int)Pass.Type)] public bool FixedCoast = true;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble FixedCoastLength = new EditableDouble(450);

        private static GUIStyle _btNormal;
        private static GUIStyle _btActive;

        public override GUILayoutOption[] WindowOptions()
        {
            return new[] { GUILayout.Width(300), GUILayout.Height(100) };
        }

        private void SetupButtonStyles()
        {
            if (_btNormal == null)
            {
                _btNormal                  = new GUIStyle(GUI.skin.button);
                _btNormal.normal.textColor = _btNormal.focused.textColor = Color.white;
                _btNormal.hover.textColor  = _btNormal.active.textColor  = Color.yellow;
                _btNormal.onNormal.textColor =
                    _btNormal.onFocused.textColor = _btNormal.onHover.textColor = _btNormal.onActive.textColor = Color.green;
                _btNormal.padding = new RectOffset(0, 0, 0, 0);

                _btActive           = new GUIStyle(_btNormal);
                _btActive.active    = _btActive.onActive;
                _btActive.normal    = _btActive.onNormal;
                _btActive.onFocused = _btActive.focused;
                _btActive.hover     = _btActive.onHover;
            }
        }

        private readonly List<bool> _optimizeToggle = new List<bool>();
        private readonly List<bool> _inertial       = new List<bool>();
        private readonly List<bool> _coastToggle    = new List<bool>();
        private          int        _optimizeStage;
        private          int        _coastStage;
        private          bool       _coastDuring;

        protected override void WindowGUI(int windowID)
        {
            SetupButtonStyles();
            ExpandStats();
            GUILayout.BeginVertical();
            GuiUtils.SimpleTextBox("Min ∆v: ", MinDeltaV, "m/s");
            ExtendIfRequired = GUILayout.Toggle(ExtendIfRequired, "Extend if Required");
            for (int i = 0; i < core.stageStats.vacStats.Length; i++)
            {
                FuelFlowSimulation.FuelStats stats = core.stageStats.vacStats[i];
                if (stats.DeltaV < MinDeltaV.val)
                {
                    if (_optimizeStage == i)
                        _optimizeStage++;
                    _inertial[i] = false;
                    continue;
                }

                string coastString = _coastDuring ? "Coast During" : "Coast Before";
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{i,3} {stats.DeltaV:##,###0} m/s");
                _optimizeToggle[i] = GUILayout.Toggle(_optimizeToggle[i],
                    i == _optimizeStage ? "Optim" : "Fixed",
                    i == _optimizeStage ? _btActive : _btNormal);
                _coastToggle[i] = GUILayout.Toggle(_coastToggle[i],
                    i == _coastStage ? coastString : "No Coast",
                    i == _coastStage ? _btActive : _btNormal);
                _inertial[i] = GUILayout.Toggle(_inertial[i],
                    _inertial[i] ? "Inertial" : "Guided",
                    _inertial[i] ? _btActive : _btNormal);

                if (_optimizeToggle[i])
                    _optimizeStage = _optimizeStage == i ? -1 : i;

                if (_coastToggle[i])
                {
                    if (_coastStage == i && !_coastDuring)
                    {
                        _coastDuring = true;
                    }
                    else
                    {
                        _coastDuring = false;
                        _coastStage  = _coastStage == i ? -1 : i;
                    }
                }

                GUILayout.EndHorizontal();
            }

            GuiUtils.SimpleTextBox("Max Coast: ", MaxCoast, "s");
            GuiUtils.ToggledTextBox(ref FixedCoast, "Fixed Coast Length:", FixedCoastLength, "s", width: 40);

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        private void ExpandStats()
        {
            while (_optimizeToggle.Count < core.stageStats.vacStats.Length)
                _optimizeToggle.Add(false);
            while (_inertial.Count < core.stageStats.vacStats.Length)
                _inertial.Add(false);
            while (_coastToggle.Count < core.stageStats.vacStats.Length)
                _coastToggle.Add(false);
            for (int i = 0; i < core.stageStats.vacStats.Length; i++)
            {
                _optimizeToggle[i] = false;
                _coastToggle[i]    = false;
            }
        }

        public override string GetName()
        {
            return "PVG Staging Editor";
        }

        public override string IconName()
        {
            return "PVG Staging Editor";
        }
    }
}
