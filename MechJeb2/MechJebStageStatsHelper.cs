using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KSP.Localization;
using UnityEngine;
using UnityEngine.Profiling;

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
        private bool showStagedMass, showBurnedMass, showInitialMass, showFinalMass, showVacInitialTWR, showAtmoInitialTWR;
        private bool showAtmoMaxTWR, showVacMaxTWR, showAtmoDeltaV, showVacDeltaV, showTime, showISP, showEmpty, timeSeconds, liveSLT;
        private int TWRBody;
        private float altSLTScale, machScale;
        private int StageDisplayState { get => infoItems.StageDisplayState; set => infoItems.StageDisplayState = value; }
        private readonly MechJebModuleInfoItems infoItems;
        private readonly MechJebCore core;
        private readonly MechJebModuleStageStats stats;
        public MechJebStageStatsHelper(MechJebModuleInfoItems items)
        {
            infoItems = items;
            core = items.core;
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
            showTime = items.showTime;
            showISP = items.showISP;
            showEmpty = items.showEmpty;
            timeSeconds = items.timeSeconds;
            liveSLT = items.liveSLT;
            altSLTScale = items.altSLTScale;
            machScale = items.machScale;
            TWRBody = items.TWRBody;

            bodies = (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor) ? FlightGlobals.Bodies.ConvertAll(b => b.GetName()).ToArray() : new [] { "None" };
            InitializeStageInfo();
            SetVisibility(StageDisplayState);
        }

        private enum StageData
        {
            InitialMass, FinalMass, StagedMass, BurnedMass, VacInitialTWR, VacMaxTWR, AtmoInitialTWR, AtmoMaxTWR, Isp, AtmoDeltaV, VacDeltaV, Time
        };
        private static readonly List<StageData> AllStages = new List<StageData>
        { 
            StageData.InitialMass, StageData.FinalMass, StageData.StagedMass, StageData.BurnedMass, StageData.VacInitialTWR, StageData.VacMaxTWR, StageData.AtmoInitialTWR, StageData.AtmoMaxTWR, StageData.Isp, StageData.AtmoDeltaV, StageData.VacDeltaV, StageData.Time
        };
        private static readonly string[] StageDisplayStates = { Localizer.Format("#MechJeb_InfoItems_button1"),Localizer.Format("#MechJeb_InfoItems_button2"),Localizer.Format("#MechJeb_InfoItems_button3"),Localizer.Format("#MechJeb_InfoItems_button4") };//"Short stats""Long stats""Full stats""Custom"

        //private FuelFlowSimulation.FuelStats[] vacStats;
        //private FuelFlowSimulation.FuelStats[] atmoStats;
        private readonly string[] bodies;

        private readonly List<int> stages = new List<int>(8);

        private readonly Dictionary<StageData,bool> stageVisibility = new Dictionary<StageData,bool>(12);
        private readonly Dictionary<StageData,List<string>> stageDisplayInfo = new Dictionary<StageData,List<string>>(12);
        private readonly Dictionary<StageData,string> stageHeaderData = new Dictionary<StageData,string>(12);

        private void InitializeStageInfo()
        {
            const string SPACING = "   ";
            stageVisibility.Clear();
            stageDisplayInfo.Clear();
            foreach (StageData ident in AllStages)
            {
                stageVisibility.Add(ident,false);
                stageDisplayInfo.Add(ident,new List<string>(16));
            }
            stageHeaderData.Clear();
            stageHeaderData.Add(StageData.InitialMass,CachedLocalizer.Instance.MechJeb_InfoItems_StatsColumn1 + SPACING);
            stageHeaderData.Add(StageData.FinalMass,CachedLocalizer.Instance.MechJeb_InfoItems_StatsColumn2 + SPACING);
            stageHeaderData.Add(StageData.StagedMass, CachedLocalizer.Instance.MechJeb_InfoItems_StatsColumn3 + SPACING);
            stageHeaderData.Add(StageData.BurnedMass, CachedLocalizer.Instance.MechJeb_InfoItems_StatsColumn4 + SPACING);
            stageHeaderData.Add(StageData.VacInitialTWR, CachedLocalizer.Instance.MechJeb_InfoItems_StatsColumn5 + SPACING);
            stageHeaderData.Add(StageData.VacMaxTWR, CachedLocalizer.Instance.MechJeb_InfoItems_StatsColumn6 + SPACING);
            stageHeaderData.Add(StageData.AtmoInitialTWR, CachedLocalizer.Instance.MechJeb_InfoItems_StatsColumn7 + SPACING);
            stageHeaderData.Add(StageData.AtmoMaxTWR, CachedLocalizer.Instance.MechJeb_InfoItems_StatsColumn8 + SPACING);
            stageHeaderData.Add(StageData.Isp, CachedLocalizer.Instance.MechJeb_InfoItems_StatsColumn9 + SPACING);
            stageHeaderData.Add(StageData.AtmoDeltaV, CachedLocalizer.Instance.MechJeb_InfoItems_StatsColumn10 + SPACING);
            stageHeaderData.Add(StageData.VacDeltaV, CachedLocalizer.Instance.MechJeb_InfoItems_StatsColumn11 + SPACING);
            stageHeaderData.Add(StageData.Time, CachedLocalizer.Instance.MechJeb_InfoItems_StatsColumn12 + SPACING);
        }

        private void GatherStages(List<int> stages)
        {
            stages.Clear();
            for (int i = 0; i < stats.atmoStats.Length; i++)
                if (infoItems.showEmpty || stats.atmoStats[i].DeltaV > 0)
                    stages.Add(i);
        }

        private void UpdateStageDisplayInfo(List<int> stages,double geeASL)
        {
            foreach (var kvp in stageDisplayInfo)
                kvp.Value.Clear();
            foreach (int index in stages)
            {
                if (stageVisibility[StageData.InitialMass]) stageDisplayInfo[StageData.InitialMass].Add($"{stats.atmoStats[index].StartMass:F3} t   ");
                if (stageVisibility[StageData.FinalMass]) stageDisplayInfo[StageData.FinalMass].Add($"{stats.atmoStats[index].EndMass:F3} t   ");
                if (stageVisibility[StageData.StagedMass]) stageDisplayInfo[StageData.StagedMass].Add($"{stats.atmoStats[index].StagedMass:F3} t   ");
                if (stageVisibility[StageData.BurnedMass]) stageDisplayInfo[StageData.BurnedMass].Add($"{stats.atmoStats[index].ResourceMass:F3} t   ");
                if (stageVisibility[StageData.VacInitialTWR]) stageDisplayInfo[StageData.VacInitialTWR].Add($"{stats.vacStats[index].StartTWR(geeASL):F2}   ");
                if (stageVisibility[StageData.VacMaxTWR]) stageDisplayInfo[StageData.VacMaxTWR].Add($"{stats.vacStats[index].MaxTWR(geeASL):F2}   ");
                if (stageVisibility[StageData.AtmoInitialTWR]) stageDisplayInfo[StageData.AtmoInitialTWR].Add($"{stats.atmoStats[index].StartTWR(geeASL):F2}   ");
                if (stageVisibility[StageData.AtmoMaxTWR]) stageDisplayInfo[StageData.AtmoMaxTWR].Add($"{stats.atmoStats[index].MaxTWR(geeASL):F2}   ");
                if (stageVisibility[StageData.Isp]) stageDisplayInfo[StageData.Isp].Add($"{stats.atmoStats[index].Isp:F2}   ");
                if (stageVisibility[StageData.AtmoDeltaV]) stageDisplayInfo[StageData.AtmoDeltaV].Add($"{stats.atmoStats[index].DeltaV:F0} m/s   ");
                if (stageVisibility[StageData.VacDeltaV]) stageDisplayInfo[StageData.VacDeltaV].Add($"{stats.vacStats[index].DeltaV:F0} m/s   ");
                if (stageVisibility[StageData.Time]) stageDisplayInfo[StageData.Time].Add(timeSeconds ? $"{stats.atmoStats[index].DeltaTime:F2}s   " : $"{GuiUtils.TimeToDHMS(stats.atmoStats[index].DeltaTime,1)}   ");
            }
        }

        private void SetAllStageVisibility(bool state)
        {
            foreach (StageData data in AllStages)
                stageVisibility[data] = state;
        }

        // This should only be called before Layout phase, and never in Repaint phase
        public void UpdateStageStats()
        {
            double geeASL = HighLogic.LoadedSceneIsEditor ? FlightGlobals.Bodies[TWRBody].GeeASL : stats.mainBody.GeeASL;
            stats.RequestUpdate(this);
            GatherStages(stages);
            UpdateStageDisplayInfo(stages,geeASL);
        }

        public void AllStageStats()
        {
            Profiler.BeginSample("AllStageStats.UI1");

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label(CachedLocalizer.Instance.MechJeb_InfoItems_label1);//"Stage stats"

            if (GUILayout.Button(timeSeconds ? "s" : "dhms",GUILayout.ExpandWidth(false)))
            {
                timeSeconds = !timeSeconds;
                infoItems.timeSeconds = timeSeconds;
            }

            if (GUILayout.Button(showEmpty ? CachedLocalizer.Instance.MechJeb_InfoItems_showEmpty : CachedLocalizer.Instance.MechJeb_InfoItems_hideEmpty,GUILayout.ExpandWidth(false)))
            {
                showEmpty = !showEmpty;
                infoItems.showEmpty = showEmpty;
            }

            if (GUILayout.Button(StageDisplayStates[StageDisplayState],GUILayout.ExpandWidth(false)))
            {
                StageDisplayState = (StageDisplayState + 1) % StageDisplayStates.Length;
                SetVisibility(StageDisplayState);
            }

            if (!HighLogic.LoadedSceneIsEditor)
            {
                if (GUILayout.Button(liveSLT ? CachedLocalizer.Instance.MechJeb_InfoItems_button5 : CachedLocalizer.Instance.MechJeb_InfoItems_button6,GUILayout.ExpandWidth(false)))//"Live SLT" "0Alt SLT"
                {
                    liveSLT = !liveSLT;
                    infoItems.liveSLT = liveSLT;
                }
                stats.liveSLT = liveSLT;
            }
            GUILayout.EndHorizontal();

            if (HighLogic.LoadedSceneIsEditor)
            {
                GUILayout.BeginHorizontal();

                TWRBody = GuiUtils.ComboBox.Box(TWRBody,bodies,this,false);
                infoItems.TWRBody = TWRBody;
                stats.editorBody = FlightGlobals.Bodies[TWRBody];

                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();
                altSLTScale = GUILayout.HorizontalSlider(altSLTScale,0,1,GUILayout.ExpandWidth(true));
                infoItems.altSLTScale = altSLTScale;
                stats.altSLT = Math.Pow(altSLTScale,2) * stats.editorBody.atmosphereDepth;
                GUILayout.Label(MuUtils.ToSI(stats.altSLT,2) + "m",GUILayout.Width(80));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                machScale = GUILayout.HorizontalSlider(machScale,0,1,GUILayout.ExpandWidth(true));
                infoItems.machScale = machScale;
                stats.mach = Math.Pow(machScale * 2,3);
                GUILayout.Label(stats.mach.ToString("F1") + " M",GUILayout.Width(80));
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
            }
            else
                stats.editorBody = stats.mainBody;

            Profiler.EndSample();

            Profiler.BeginSample("AllStageStats.DrawColumns");

            GUILayout.BeginHorizontal();
            DrawStageStatsColumn(CachedLocalizer.Instance.MechJeb_InfoItems_StatsColumn0,stages.Select(s => s.ToString()).ToList());

            bool buttonNotPressed = true;
            foreach (StageData info in AllStages)
            {
                if (stageVisibility[info]) buttonNotPressed &= stageVisibility[info] = !DrawStageStatsColumn(stageHeaderData[info], stageDisplayInfo[info]);
            }

            if (!buttonNotPressed)
            {
                StageDisplayState = 3;
                SaveStageVisibility();
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            Profiler.EndSample();
        }

        static GUIStyle _columnStyle;
        public static GUIStyle ColumnStyle
        {
            get
            {
                return _columnStyle ??= new GUIStyle(GuiUtils.yellowOnHover)
                {
                    alignment = TextAnchor.MiddleRight,
                    wordWrap = false,
                    padding = new RectOffset(2,2,0,0)
                };
            }
        }

        bool DrawStageStatsColumn(string header,in List<string> data)
        {
            GUILayout.BeginVertical();
            bool ret = GUILayout.Button(header,ColumnStyle);
            foreach (string datum in data) GUILayout.Label(datum,ColumnStyle);
            GUILayout.EndVertical();
            return ret;
        }

        private void LoadStageVisibility()
        {
            stageVisibility[StageData.StagedMass] = showStagedMass;
            stageVisibility[StageData.BurnedMass] = showBurnedMass;
            stageVisibility[StageData.InitialMass] = showInitialMass;
            stageVisibility[StageData.FinalMass] = showFinalMass;
            stageVisibility[StageData.VacInitialTWR] = showVacInitialTWR;
            stageVisibility[StageData.AtmoInitialTWR] = showAtmoInitialTWR;
            stageVisibility[StageData.AtmoMaxTWR] = showAtmoMaxTWR;
            stageVisibility[StageData.VacMaxTWR] = showVacMaxTWR;
            stageVisibility[StageData.AtmoDeltaV] = showAtmoDeltaV;
            stageVisibility[StageData.VacDeltaV] = showVacDeltaV;
            stageVisibility[StageData.Time] = showTime;
            stageVisibility[StageData.Isp] = showISP;
        }

        private void SaveStageVisibility()
        {
            showStagedMass = infoItems.showStagedMass = stageVisibility[StageData.StagedMass];
            showBurnedMass = infoItems.showBurnedMass = stageVisibility[StageData.BurnedMass];
            showInitialMass = infoItems.showInitialMass = stageVisibility[StageData.InitialMass];
            showFinalMass = infoItems.showFinalMass = stageVisibility[StageData.FinalMass];
            showVacInitialTWR = infoItems.showVacInitialTWR = stageVisibility[StageData.VacInitialTWR];
            showAtmoInitialTWR = infoItems.showAtmoInitialTWR = stageVisibility[StageData.AtmoInitialTWR];
            showAtmoMaxTWR = infoItems.showAtmoMaxTWR = stageVisibility[StageData.AtmoMaxTWR];
            showVacMaxTWR = infoItems.showVacMaxTWR = stageVisibility[StageData.VacMaxTWR];
            showAtmoDeltaV = infoItems.showAtmoDeltaV = stageVisibility[StageData.AtmoDeltaV];
            showVacDeltaV = infoItems.showVacDeltaV = stageVisibility[StageData.VacDeltaV];
            showTime = infoItems.showTime = stageVisibility[StageData.Time];
            showISP = infoItems.showISP = stageVisibility[StageData.Isp];
        }

        private void SetVisibility(int state)
        {
            switch (state)
            {
                case 0:
                    SetAllStageVisibility(false);
                    stageVisibility[StageData.VacInitialTWR] = true;
                    stageVisibility[StageData.AtmoInitialTWR] = true;
                    stageVisibility[StageData.VacDeltaV] = true;
                    stageVisibility[StageData.AtmoDeltaV] = true;
                    stageVisibility[StageData.Time] = true;
                    break;
                case 1:
                    SetAllStageVisibility(true);
                    stageVisibility[StageData.StagedMass] = false;
                    stageVisibility[StageData.BurnedMass] = false;
                    stageVisibility[StageData.Isp] = false;
                    break;
                case 2:
                    SetAllStageVisibility(true);
                    break;
                case 3:
                    LoadStageVisibility();
                    break;
            }
        }
    }
}
