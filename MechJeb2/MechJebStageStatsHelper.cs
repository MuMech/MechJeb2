using System;
using System.Collections.Generic;
using KSP.Localization;
using MechJebLibBindings;
using UnityEngine;
using UnityEngine.Profiling;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    // Stage Stats display is much too complicated to bury in a single method in MechJebModuleInfoItems
    // However, that's where it lived, so there is a lot of persistent state currently stored there.
    // I'm not familiar enough with how to un-bury a lot of that data.
    // This helper class will load all the persistent data from the MechJebModuleInfoItems, and then operate on its own.
    // It will write back changes to that data store.
    // Eventually, we should figure out how to not need that store at all.
    public class MechJebStageStatsHelper
    {
        private bool showStagedMass, showBurnedMass, showInitialMass, showFinalMass, showThrust, showVacInitialTWR, showAtmoInitialTWR;
        private bool showAtmoMaxTWR, showVacMaxTWR, showAtmoDeltaV, showVacDeltaV, showTime, showISP, showEmpty, showRcs, timeSeconds, liveSLT;
        private bool showAtmoCumulativeDeltaV, showVacCumulativeDeltaV, showControllableMass, showRcsUllageTime;
        private int TWRBody;
        private float altSLTScale, machScale;
        private int StageDisplayState { get => infoItems.StageDisplayState; set => infoItems.StageDisplayState = value; }
        private readonly MechJebModuleInfoItems infoItems;
        private readonly MechJebCore core;
        private readonly MechJebModuleStageStats stats;

        public MechJebStageStatsHelper(MechJebModuleInfoItems items)
        {
            infoItems = items;
            core = items.Core;
            stats = core.GetComputerModule<MechJebModuleStageStats>();
            showStagedMass = items.showStagedMass;
            showBurnedMass = items.showBurnedMass;
            showInitialMass = items.showInitialMass;
            showFinalMass = items.showFinalMass;
            showVacInitialTWR = items.showVacInitialTWR;
            showAtmoInitialTWR = items.showAtmoInitialTWR;
            showAtmoMaxTWR = items.showAtmoMaxTWR;
            showVacMaxTWR = items.showVacMaxTWR;
            showAtmoDeltaV = items.showAtmoDeltaV;
            showVacDeltaV = items.showVacDeltaV;
            showAtmoCumulativeDeltaV = items.showAtmoCumulativeDeltaV;
            showVacCumulativeDeltaV = items.showVacCumulativeDeltaV;
            showControllableMass = items.showControllableMass;
            showRcsUllageTime = items.showRcsUllageTime;
            showTime = items.showTime;
            showISP = items.showISP;
            showThrust = items.showThrust;
            showRcs = items.showRcs;
            showEmpty = items.showEmpty;
            timeSeconds = items.timeSeconds;
            liveSLT = items.liveSLT;
            altSLTScale = items.altSLTScale;
            machScale = items.machScale;
            TWRBody = items.TWRBody;

            bodies = HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor
                ? FlightGlobals.Bodies.ConvertAll(b => b.GetName()).ToArray()
                : new[] { "None" };
            InitializeStageInfo();
            SetVisibility(StageDisplayState);
        }

        internal enum StageData
        {
            KSPStage, InitialMass, FinalMass, StagedMass, BurnedMass, Thrust, VacInitialTWR, VacMaxTWR, AtmoInitialTWR, AtmoMaxTWR,
            Isp, AtmoDeltaV, VacDeltaV, Time, AtmoCumulativeDeltaV, VacCumulativeDeltaV, ControllableMass, RcsUllageTime
        }

        // ---- read access and actions for another UI (MechJeb2.UI's stage stats table); AllStageStats uses the same actions ----

        internal static IReadOnlyList<StageData> Columns => AllStages;
        internal static string[] DisplayStateNames => StageDisplayStates;
        internal string[] Bodies => bodies;
        internal List<int> Stages => stages;
        internal bool IsVisible(StageData d) => stageVisibility[d];
        internal string Header(StageData d) => stageHeaderData[d];
        internal bool TimeSeconds => timeSeconds;
        internal bool ShowEmpty => showEmpty;
        internal bool ShowRcs => showRcs;
        internal bool LiveSLT => liveSLT;
        internal int DisplayState => StageDisplayState;
        internal int TwrBody { get => TWRBody; set => TWRBody = infoItems.TWRBody = value; }
        internal float AltSLTScale { get => altSLTScale; set => altSLTScale = infoItems.altSLTScale = value; }
        internal float MachScale { get => machScale; set => machScale = infoItems.machScale = value; }
        internal double AltSLT => stats.AltSLT;
        internal double Mach => stats.Mach;

        internal void ToggleTimeSeconds() => timeSeconds = infoItems.timeSeconds = !timeSeconds;
        internal void ToggleShowEmpty() => showEmpty = infoItems.showEmpty = !showEmpty;

        internal void CycleDisplayState()
        {
            StageDisplayState = (StageDisplayState + 1) % StageDisplayStates.Length;
            SetVisibility(StageDisplayState);
        }

        internal void ToggleRcs()
        {
            showRcs = !showRcs;
            infoItems.showRcs = showRcs;
            InitalizeStageHeaderData();
            SetVisibility(StageDisplayState);
        }

        internal void ToggleLiveSLT() => liveSLT = infoItems.liveSLT = !liveSLT;

        /// <summary>Clicking a column header hides it and switches to the "Custom" display state.</summary>
        internal void HideColumn(StageData d)
        {
            if (d == StageData.KSPStage) return;
            stageVisibility[d] = false;
            StageDisplayState = 3;
            SaveStageVisibility();
        }

        /// <summary>Push the environment the stats are computed for (live SLT in flight; body, altitude and mach in the editor).</summary>
        internal void ApplyEnvironment()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                stats.EditorBody = FlightGlobals.Bodies[TWRBody];
                stats.AltSLT = Math.Pow(altSLTScale, 2) * stats.EditorBody.atmosphereDepth;
                stats.Mach = Math.Pow(machScale * 2, 3);
            }
            else
            {
                stats.LiveSLT = liveSLT;
                stats.EditorBody = stats.MainBody;
            }
        }

        internal double GeeASL => HighLogic.LoadedSceneIsEditor ? FlightGlobals.Bodies[TWRBody].GeeASL : stats.MainBody.GeeASL;

        /// <summary>UpdateStageStats without building the display strings.</summary>
        internal void UpdateStagesOnly()
        {
            stats.RequestUpdate();
            GatherStages(stages);
        }

        /// <summary>
        /// The number shown in column d for the stage at position row of Stages, and how UpdateStageDisplayInfo formats it:
        /// decimals ≥ 0 → "F{decimals}" + unit; -1 → a time (F2 + "s" with TimeSeconds, else DHMS with 1 decimal) + unit.
        /// </summary>
        internal double Value(StageData d, int row, double geeASL, out int decimals, out string unit)
        {
            int index = stages[row];
            unit = "   ";
            decimals = 2;
            switch (d)
            {
                case StageData.KSPStage:
                    decimals = 0;
                    return stats.AtmoStats[index].KSPStage;
                case StageData.ControllableMass:
                    decimals = 3; unit = " t   ";
                    return stats.AtmoStats[index].ControllableMass;
                case StageData.InitialMass:
                    decimals = 3; unit = " t   ";
                    return stats.AtmoStats[index].StartMass;
                case StageData.FinalMass:
                    decimals = 3; unit = " t   ";
                    return stats.AtmoStats[index].EndMass;
                case StageData.StagedMass:
                    decimals = 3; unit = " t   ";
                    return stats.AtmoStats[index].StagedMass;
                case StageData.BurnedMass:
                    decimals = 3; unit = " t   ";
                    return _burnedMass(index);
                case StageData.Thrust:
                    decimals = 3; unit = " kN   ";
                    return _thrust(index);
                case StageData.VacInitialTWR: return _vacStartTWR(index, geeASL);
                case StageData.VacMaxTWR: return _vacEndTWR(index, geeASL);
                case StageData.AtmoInitialTWR: return _atmoStartTWR(index, geeASL);
                case StageData.AtmoMaxTWR: return _atmoEndTWR(index, geeASL);
                case StageData.Isp: return _isp(index);
                case StageData.RcsUllageTime:
                    decimals = -1;
                    return stats.AtmoStats[index].RcsUllageTime;
                case StageData.AtmoDeltaV:
                    decimals = 0; unit = " m/s   ";
                    return _atmoDv(index);
                case StageData.VacDeltaV:
                    decimals = 0; unit = " m/s   ";
                    return _vacDv(index);
                case StageData.AtmoCumulativeDeltaV:
                    decimals = 0; unit = " m/s   ";
                    return CalculateCumulativeAtmoDeltaV(stages, row);
                case StageData.VacCumulativeDeltaV:
                    decimals = 0; unit = " m/s   ";
                    return CalculateCumulativeVacDeltaV(stages, row);
                case StageData.Time:
                    decimals = -1;
                    return _deltaTime(index);
            }

            return 0;
        }

        private static readonly List<StageData> AllStages = new List<StageData>
        {
            StageData.KSPStage,
            StageData.ControllableMass,
            StageData.InitialMass,
            StageData.FinalMass,
            StageData.StagedMass,
            StageData.BurnedMass,
            StageData.Thrust,
            StageData.VacInitialTWR,
            StageData.VacMaxTWR,
            StageData.AtmoInitialTWR,
            StageData.AtmoMaxTWR,
            StageData.Isp,
            StageData.RcsUllageTime,
            StageData.AtmoCumulativeDeltaV,
            StageData.VacCumulativeDeltaV,
            StageData.AtmoDeltaV,
            StageData.VacDeltaV,
            StageData.Time
        };

        private static readonly string[] StageDisplayStates = { Localizer.Format("#MechJeb_InfoItems_button1"), Localizer.Format("#MechJeb_InfoItems_button2"), Localizer.Format("#MechJeb_InfoItems_button3"), Localizer.Format("#MechJeb_InfoItems_button4") }; //"Short stats""Long stats""Full stats""Custom"

        //private FuelFlowSimulation.FuelStats[] vacStats;
        //private FuelFlowSimulation.FuelStats[] atmoStats;
        private readonly string[] bodies;

        private readonly List<int> stages = new List<int>(8);

        private readonly Dictionary<StageData, bool> stageVisibility = new Dictionary<StageData, bool>(12);
        private readonly Dictionary<StageData, List<string>> stageDisplayInfo = new Dictionary<StageData, List<string>>(12);
        private readonly Dictionary<StageData, string> stageHeaderData = new Dictionary<StageData, string>(12);

        private void InitializeStageInfo()
        {
            stageVisibility.Clear();
            stageDisplayInfo.Clear();
            foreach (StageData ident in AllStages)
            {
                stageVisibility.Add(ident, false);
                stageDisplayInfo.Add(ident, new List<string>(16));
            }

            InitalizeStageHeaderData();
        }

        private void InitalizeStageHeaderData()
        {
            const string SPACING = "   ";

            stageHeaderData.Clear();
            stageHeaderData.Add(StageData.KSPStage, "Stage" + SPACING);
            stageHeaderData.Add(StageData.ControllableMass, "Avionics" + SPACING);
            stageHeaderData.Add(StageData.InitialMass, CachedLocalizer.Instance.MechJebInfoItemsStatsColumn1 + SPACING);
            stageHeaderData.Add(StageData.FinalMass, CachedLocalizer.Instance.MechJebInfoItemsStatsColumn2 + SPACING);
            stageHeaderData.Add(StageData.StagedMass, CachedLocalizer.Instance.MechJebInfoItemsStatsColumn3 + SPACING);
            stageHeaderData.Add(StageData.BurnedMass, CachedLocalizer.Instance.MechJebInfoItemsStatsColumn4 + SPACING);
            stageHeaderData.Add(StageData.Thrust, CachedLocalizer.Instance.MechJebInfoItemsStatsColumn13 + SPACING);
            stageHeaderData.Add(StageData.VacInitialTWR, CachedLocalizer.Instance.MechJebInfoItemsStatsColumn5 + SPACING);
            stageHeaderData.Add(StageData.VacMaxTWR, CachedLocalizer.Instance.MechJebInfoItemsStatsColumn6 + SPACING);
            stageHeaderData.Add(StageData.AtmoInitialTWR, CachedLocalizer.Instance.MechJebInfoItemsStatsColumn7 + SPACING);
            stageHeaderData.Add(StageData.AtmoMaxTWR, CachedLocalizer.Instance.MechJebInfoItemsStatsColumn8 + SPACING);
            stageHeaderData.Add(StageData.Isp, CachedLocalizer.Instance.MechJebInfoItemsStatsColumn9 + SPACING);
            stageHeaderData.Add(StageData.RcsUllageTime, "RCS Ullage" + SPACING);
            stageHeaderData.Add(StageData.AtmoDeltaV, (showRcs ? "RCS ΔVmin" : CachedLocalizer.Instance.MechJebInfoItemsStatsColumn10) + SPACING);
            stageHeaderData.Add(StageData.VacDeltaV, (showRcs ? "RCS ΔVmax" : CachedLocalizer.Instance.MechJebInfoItemsStatsColumn11) + SPACING);
            stageHeaderData.Add(StageData.AtmoCumulativeDeltaV, "Σ Atmo ΔV" + SPACING);
            stageHeaderData.Add(StageData.VacCumulativeDeltaV, "Σ Vac ΔV" + SPACING);
            stageHeaderData.Add(StageData.Time, CachedLocalizer.Instance.MechJebInfoItemsStatsColumn12 + SPACING);
        }

        private void GatherStages(List<int> stages)
        {
            stages.Clear();
            for (int i = 0; i < stats.AtmoStats.Count; i++)
                if (infoItems.showEmpty)
                    stages.Add(i);
                else if (infoItems.showRcs && stats.AtmoStats[i].MinRcsDeltaV > 0)
                    stages.Add(i);
                else if (!infoItems.showRcs && stats.AtmoStats[i].DeltaV > 0)
                    stages.Add(i);
        }

        private double _isp(int index)        => showRcs ? stats.VacStats[index].RcsISP : stats.VacStats[index].Isp;
        private double _thrust(int index)     => showRcs ? stats.VacStats[index].RcsThrust : stats.VacStats[index].Thrust;
        private double _burnedMass(int index) => showRcs ? stats.VacStats[index].RcsMass : stats.VacStats[index].ResourceMass;
        private double _deltaTime(int index)  => showRcs ? stats.VacStats[index].RcsDeltaTime : stats.VacStats[index].DeltaTime;
        private double _atmoDv(int index)     => showRcs ? stats.VacStats[index].MinRcsDeltaV : stats.AtmoStats[index].DeltaV;
        private double _vacDv(int index)      => showRcs ? stats.VacStats[index].MaxRcsDeltaV : stats.VacStats[index].DeltaV;

        private double _vacStartTWR(int index, double geeASL) =>
            showRcs ? stats.VacStats[index].RcsStartTWR(geeASL) : stats.VacStats[index].StartTWR(geeASL);

        private double _vacEndTWR(int index, double geeASL) =>
            showRcs ? stats.VacStats[index].RcsMaxTWR(geeASL) : stats.VacStats[index].MaxTWR(geeASL);

        private double _atmoStartTWR(int index, double geeASL) =>
            showRcs ? stats.AtmoStats[index].RcsStartTWR(geeASL) : stats.AtmoStats[index].StartTWR(geeASL);

        private double _atmoEndTWR(int index, double geeASL) =>
            showRcs ? stats.AtmoStats[index].RcsMaxTWR(geeASL) : stats.AtmoStats[index].MaxTWR(geeASL);

        private double CalculateCumulativeVacDeltaV(List<int> stages, int currentStageIndex)
        {
            double cumulative = 0.0;

            for (int i = currentStageIndex; i < stages.Count; i++)
                cumulative += _vacDv(stages[i]);

            return cumulative;
        }

        private double CalculateCumulativeAtmoDeltaV(List<int> stages, int currentStageIndex)
        {
            double cumulative = 0.0;

            for (int i = currentStageIndex; i < stages.Count; i++)
                cumulative += _atmoDv(stages[i]);

            return cumulative;
        }

        private void UpdateStageDisplayInfo(List<int> stages, double geeASL)
        {
            foreach (KeyValuePair<StageData, List<string>> kvp in stageDisplayInfo)
                kvp.Value.Clear();
            foreach (int index in stages)
            {
                int currentStageIndex = stages.IndexOf(index);

                stageDisplayInfo[StageData.KSPStage].Add($"{stats.AtmoStats[index].KSPStage}   ");
                if (stageVisibility[StageData.ControllableMass]) stageDisplayInfo[StageData.ControllableMass].Add($"{stats.AtmoStats[index].ControllableMass:F3} t   ");
                if (stageVisibility[StageData.InitialMass])
                    stageDisplayInfo[StageData.InitialMass].Add($"{stats.AtmoStats[index].StartMass:F3} t   ");
                if (stageVisibility[StageData.FinalMass]) stageDisplayInfo[StageData.FinalMass].Add($"{stats.AtmoStats[index].EndMass:F3} t   ");
                if (stageVisibility[StageData.StagedMass]) stageDisplayInfo[StageData.StagedMass].Add($"{stats.AtmoStats[index].StagedMass:F3} t   ");
                if (stageVisibility[StageData.BurnedMass])
                    stageDisplayInfo[StageData.BurnedMass].Add($"{_burnedMass(index):F3} t   ");
                if (stageVisibility[StageData.Thrust]) stageDisplayInfo[StageData.Thrust].Add($"{_thrust(index):F3} kN   ");
                if (stageVisibility[StageData.VacInitialTWR])
                    stageDisplayInfo[StageData.VacInitialTWR].Add($"{_vacStartTWR(index, geeASL):F2}   ");
                if (stageVisibility[StageData.VacMaxTWR]) stageDisplayInfo[StageData.VacMaxTWR].Add($"{_vacEndTWR(index, geeASL):F2}   ");
                if (stageVisibility[StageData.AtmoInitialTWR])
                    stageDisplayInfo[StageData.AtmoInitialTWR].Add($"{_atmoStartTWR(index, geeASL):F2}   ");
                if (stageVisibility[StageData.AtmoMaxTWR])
                    stageDisplayInfo[StageData.AtmoMaxTWR].Add($"{_atmoEndTWR(index, geeASL):F2}   ");
                if (stageVisibility[StageData.Isp]) stageDisplayInfo[StageData.Isp].Add($"{_isp(index):F2}   ");
                if (stageVisibility[StageData.RcsUllageTime])
                    stageDisplayInfo[StageData.RcsUllageTime].Add(timeSeconds
                        ? $"{stats.AtmoStats[index].RcsUllageTime:F2}s   "
                        : $"{GuiUtils.TimeToDHMS(stats.AtmoStats[index].RcsUllageTime, 1)}   ");
                if (stageVisibility[StageData.AtmoDeltaV]) stageDisplayInfo[StageData.AtmoDeltaV].Add($"{_atmoDv(index):F0} m/s   ");
                if (stageVisibility[StageData.VacDeltaV]) stageDisplayInfo[StageData.VacDeltaV].Add($"{_vacDv(index):F0} m/s   ");
                if (stageVisibility[StageData.AtmoCumulativeDeltaV])
                    stageDisplayInfo[StageData.AtmoCumulativeDeltaV].Add($"{CalculateCumulativeAtmoDeltaV(stages, currentStageIndex):F0} m/s   ");
                if (stageVisibility[StageData.VacCumulativeDeltaV])
                    stageDisplayInfo[StageData.VacCumulativeDeltaV].Add($"{CalculateCumulativeVacDeltaV(stages, currentStageIndex):F0} m/s   ");
                if (stageVisibility[StageData.Time])
                    stageDisplayInfo[StageData.Time].Add(timeSeconds
                        ? $"{_deltaTime(index):F2}s   "
                        : $"{GuiUtils.TimeToDHMS(_deltaTime(index), 1)}   ");
            }
        }

        private void SetAllStageVisibility(bool state)
        {
            foreach (StageData data in AllStages)
                stageVisibility[data] = state;

            stageVisibility[StageData.KSPStage] = true;
        }

        // This should only be called before Layout phase, and never in Repaint phase
        public void UpdateStageStats()
        {
            double geeASL = HighLogic.LoadedSceneIsEditor ? FlightGlobals.Bodies[TWRBody].GeeASL : stats.MainBody.GeeASL;
            stats.RequestUpdate();
            GatherStages(stages);
            UpdateStageDisplayInfo(stages, geeASL);
        }

        private void LoadStageVisibility()
        {
            stageVisibility[StageData.StagedMass] = showStagedMass;
            stageVisibility[StageData.BurnedMass] = showBurnedMass;
            stageVisibility[StageData.InitialMass] = showInitialMass;
            stageVisibility[StageData.FinalMass] = showFinalMass;
            stageVisibility[StageData.Thrust] = showThrust;
            stageVisibility[StageData.VacInitialTWR] = showVacInitialTWR;
            stageVisibility[StageData.AtmoInitialTWR] = showAtmoInitialTWR;
            stageVisibility[StageData.AtmoMaxTWR] = showAtmoMaxTWR;
            stageVisibility[StageData.VacMaxTWR] = showVacMaxTWR;
            stageVisibility[StageData.AtmoDeltaV] = showAtmoDeltaV;
            stageVisibility[StageData.VacDeltaV] = showVacDeltaV;
            stageVisibility[StageData.Time] = showTime;
            stageVisibility[StageData.Isp] = showISP;
            stageVisibility[StageData.AtmoCumulativeDeltaV] = showAtmoCumulativeDeltaV;
            stageVisibility[StageData.VacCumulativeDeltaV] = showVacCumulativeDeltaV;
            stageVisibility[StageData.ControllableMass] = showControllableMass;
            stageVisibility[StageData.RcsUllageTime] = showRcsUllageTime;
        }

        private void SaveStageVisibility()
        {
            showStagedMass = infoItems.showStagedMass = stageVisibility[StageData.StagedMass];
            showBurnedMass = infoItems.showBurnedMass = stageVisibility[StageData.BurnedMass];
            showInitialMass = infoItems.showInitialMass = stageVisibility[StageData.InitialMass];
            showFinalMass = infoItems.showFinalMass = stageVisibility[StageData.FinalMass];
            showThrust = infoItems.showThrust = stageVisibility[StageData.Thrust];
            showVacInitialTWR = infoItems.showVacInitialTWR = stageVisibility[StageData.VacInitialTWR];
            showAtmoInitialTWR = infoItems.showAtmoInitialTWR = stageVisibility[StageData.AtmoInitialTWR];
            showAtmoMaxTWR = infoItems.showAtmoMaxTWR = stageVisibility[StageData.AtmoMaxTWR];
            showVacMaxTWR = infoItems.showVacMaxTWR = stageVisibility[StageData.VacMaxTWR];
            showAtmoDeltaV = infoItems.showAtmoDeltaV = stageVisibility[StageData.AtmoDeltaV];
            showVacDeltaV = infoItems.showVacDeltaV = stageVisibility[StageData.VacDeltaV];
            showTime = infoItems.showTime = stageVisibility[StageData.Time];
            showISP = infoItems.showISP = stageVisibility[StageData.Isp];
            showAtmoCumulativeDeltaV = infoItems.showAtmoCumulativeDeltaV = stageVisibility[StageData.AtmoCumulativeDeltaV];
            showVacCumulativeDeltaV = infoItems.showVacCumulativeDeltaV = stageVisibility[StageData.VacCumulativeDeltaV];
            showControllableMass = infoItems.showControllableMass = stageVisibility[StageData.ControllableMass];
            showRcsUllageTime = infoItems.showRcsUllageTime = stageVisibility[StageData.RcsUllageTime];
        }

        private void SetVisibility(int state)
        {
            switch (state)
            {
                case 0:
                    SetAllStageVisibility(false);
                    stageVisibility[StageData.ControllableMass] = ReflectionUtils.IsLoadedRP0;
                    stageVisibility[StageData.VacInitialTWR] = true;
                    stageVisibility[StageData.AtmoInitialTWR] = true;
                    stageVisibility[StageData.VacCumulativeDeltaV] = true;
                    stageVisibility[StageData.VacDeltaV] = true;
                    stageVisibility[StageData.AtmoDeltaV] = true;
                    stageVisibility[StageData.Time] = true;
                    break;
                case 1:
                    SetAllStageVisibility(true);
                    stageVisibility[StageData.ControllableMass] = ReflectionUtils.IsLoadedRP0;
                    stageVisibility[StageData.RcsUllageTime] = ReflectionUtils.IsLoadedRP0;
                    stageVisibility[StageData.AtmoCumulativeDeltaV] = false;
                    stageVisibility[StageData.AtmoMaxTWR] = false;
                    stageVisibility[StageData.Thrust] = false;
                    stageVisibility[StageData.StagedMass] = false;
                    stageVisibility[StageData.BurnedMass] = false;
                    stageVisibility[StageData.Isp] = false;
                    break;
                case 2:
                    SetAllStageVisibility(true);
                    stageVisibility[StageData.RcsUllageTime] = ReflectionUtils.IsLoadedRP0;
                    stageVisibility[StageData.ControllableMass] = ReflectionUtils.IsLoadedRP0;
                    break;
                case 3:
                    LoadStageVisibility();
                    break;
            }
        }
    }
}
