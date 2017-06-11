using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionSmartASS : MechJebModuleScriptAction
	{
		public static String NAME = "SmartASS";
		private MechJebModuleSmartASS smartAss;

		private static MechJebModuleSmartASS.Mode[] Target2Mode = MechJebModuleSmartASS.Target2Mode;
		private static bool[] TargetIsMode = MechJebModuleSmartASS.TargetIsMode;
		private static string[] ModeTexts = MechJebModuleSmartASS.ScriptModeTexts;
		private static string[] TargetTexts = MechJebModuleSmartASS.ScriptTargetTexts;

		// Target/Mode -> Mode index
		private int[] targetModeSelectorIdxs = new int[TargetTexts.Length];
		private int[] modeModeSelectorIdxs   = new int[ModeTexts.Length];
		// Mode index -> Mode/Target
		private List<bool> isTargetMode                        = new List<bool>();
		private List<MechJebModuleSmartASS.Target> targetModes = new List<MechJebModuleSmartASS.Target>();
		private List<MechJebModuleSmartASS.Mode> modeModes     = new List<MechJebModuleSmartASS.Mode>();
		private List<String> modeStrings                       = new List<String>();

		// Target -> Target index
		private int[] targetSelectorIdxs = new int[TargetTexts.Length];
		// Mode -> Target index -> Target
		private List<MechJebModuleSmartASS.Target>[] targets = new List<MechJebModuleSmartASS.Target>[ModeTexts.Length];
		private List<String>[] targetStrings                 = new List<String>[ModeTexts.Length];

		private static string[] ReferenceTexts = Enum.GetNames(typeof(AttitudeReference));
		private static string[] DirectionTexts = Enum.GetNames(typeof(Vector6.Direction));

		private MechJebModuleSmartASS.Target target;
		[Persistent(pass = (int)Pass.Type)]
		private String targetName;
		[Persistent(pass = (int)Pass.Type)]
		public EditableDouble srfHdg = new EditableDouble(90);
		[Persistent(pass = (int)Pass.Type)]
		public EditableDouble srfPit = new EditableDouble(90);
		[Persistent(pass = (int)Pass.Type)]
		public EditableDouble srfRol = new EditableDouble(0);
		[Persistent(pass = (int)Pass.Type)]
		public EditableDouble srfVelYaw = new EditableDouble(0);
		[Persistent(pass = (int)Pass.Type)]
		public EditableDouble srfVelPit = new EditableDouble(0);
		[Persistent(pass = (int)Pass.Type)]
		public EditableDouble srfVelRol = new EditableDouble(0);
		[Persistent(pass = (int)Pass.Type)]
		public EditableDouble rol = new EditableDouble(0);
		[Persistent(pass = (int)Pass.Type)]
		public AttitudeReference advReference = AttitudeReference.INERTIAL;
		[Persistent(pass = (int)Pass.Type)]
		public Vector6.Direction advDirection = Vector6.Direction.FORWARD;
		[Persistent(pass = (int)Pass.Type)]
		private Boolean forceRol = false;
		[Persistent(pass = (int)Pass.Type)]
		private Boolean forcePitch = true;
		[Persistent(pass = (int)Pass.Type)]
		private Boolean forceYaw = true;

		public MechJebModuleScriptActionSmartASS (MechJebModuleScript scriptModule, MechJebCore core):base(scriptModule, core, NAME)
		{
			this.smartAss = core.GetComputerModule<MechJebModuleSmartASS>();
			int modeIdx = 0;
			foreach (MechJebModuleSmartASS.Target target in Enum.GetValues(typeof(MechJebModuleSmartASS.Target)))
			{
				if (TargetIsMode[(int)target] && target != MechJebModuleSmartASS.Target.AUTO)
				{
					isTargetMode.Add(true);
					modeModes.Add((MechJebModuleSmartASS.Mode)0);
					targetModes.Add(target);
					modeStrings.Add(TargetTexts[(int)target]);
					targetModeSelectorIdxs[(int)target] = modeIdx++;
				}
			}

			foreach (MechJebModuleSmartASS.Mode mode in Enum.GetValues(typeof(MechJebModuleSmartASS.Mode)))
			{
				if (mode != MechJebModuleSmartASS.Mode.AUTO)
				{
					modeModes.Add(mode);
					isTargetMode.Add(false);
					targetModes.Add((MechJebModuleSmartASS.Target)0);
					modeStrings.Add(ModeTexts[(int)mode]);
					modeModeSelectorIdxs[(int)mode] = modeIdx++;
					targets[(int)mode] = new List<MechJebModuleSmartASS.Target>();
					targetStrings[(int)mode] = new List<String>();
				}
			}

			foreach (MechJebModuleSmartASS.Target target in Enum.GetValues(typeof(MechJebModuleSmartASS.Target)))
			{
				if (!TargetIsMode[(int)target])
				{
					MechJebModuleSmartASS.Mode mode = Target2Mode[(int)target];
					targetSelectorIdxs[(int)target] = targets[(int)mode].Count;
					targets[(int)mode].Add(target);
					targetStrings[(int)mode].Add(TargetTexts[(int)target]);
				}
			}
		}

		override public void activateAction(int actionIndex)
		{
			base.activateAction(actionIndex);
			this.smartAss.mode = Target2Mode[(int)target];
			this.smartAss.target = target;
			this.smartAss.srfHdg = this.srfHdg;
			this.smartAss.srfPit = this.srfPit;
			this.smartAss.srfRol = this.srfRol;
			this.smartAss.srfVelYaw = this.srfVelYaw;
			this.smartAss.srfVelPit = this.srfVelPit;
			this.smartAss.srfVelRol = this.srfVelRol;
			this.smartAss.rol = this.rol;
			this.smartAss.advReference = this.advReference;
			this.smartAss.advDirection = this.advDirection;
			this.smartAss.forceRol = this.forceRol;
			this.smartAss.forcePitch = this.forcePitch;
			this.smartAss.forceYaw = this.forceYaw;
			this.smartAss.Engage();
		}

		override public void endAction()
		{
			base.endAction();
		}

		override public void postLoad(ConfigNode node)
		{
			foreach (MechJebModuleSmartASS.Target target in Enum.GetValues(typeof(MechJebModuleSmartASS.Target)))
			{
				if (TargetTexts[(int)target].CompareTo(this.targetName) == 0)
				{
					this.target = target;
					break;
				}
			}
		}

		override public void afterOnFixedUpdate()
		{
			if (!this.isStarted() || this.isExecuted())
			{
				return;
			}
			if (this.target == MechJebModuleSmartASS.Target.KILLROT)
			{
				if (core.vesselState.angularVelocity.magnitude < 0.002)
				{
					this.endAction();
				}
			}
			else if (core.attitude.attitudeAngleFromTarget() < 2 && core.vesselState.angularVelocity.magnitude < 0.02)
			{
				this.endAction();
			}
		}

		override public void WindowGUI(int windowID)
		{
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);
			int modeSelectorIdx = TargetIsMode[(int)target]
				? targetModeSelectorIdxs[(int)target]
				: modeModeSelectorIdxs[(int)Target2Mode[(int)target]];
			GUILayout.BeginHorizontal(GUILayout.Width(150));
			modeSelectorIdx = GuiUtils.ComboBox.Box(modeSelectorIdx, modeStrings.ToArray(), modeStrings);
			GUILayout.EndHorizontal();
			bool showForceRol = false;
			if (this.isTargetMode[modeSelectorIdx])
			{
				target = this.targetModes[modeSelectorIdx];
				showForceRol = (target == MechJebModuleSmartASS.Target.NODE);
			}
			else
			{
				MechJebModuleSmartASS.Mode mode = this.modeModes[modeSelectorIdx];
				List<String> targetStrings = this.targetStrings[(int)mode];
				if (targetStrings.Count == 1)
				{
					target = this.targets[(int)mode][0];
				}
				else
				{
					int targetSelectorIdx = (Target2Mode[(int)target] == mode) ? targetSelectorIdxs[(int)target] : 0;
					GUILayout.BeginHorizontal(GUILayout.Width(160));
					targetSelectorIdx = GuiUtils.ComboBox.Box(targetSelectorIdx, targetStrings.ToArray(), targetStrings);
					GUILayout.EndHorizontal();
					target = this.targets[(int)mode][targetSelectorIdx];
				}
				switch(mode)
				{
					case MechJebModuleSmartASS.Mode.ORBITAL:
					case MechJebModuleSmartASS.Mode.TARGET:
						showForceRol = true;
						break;
					case MechJebModuleSmartASS.Mode.ADVANCED:
						GUILayout.BeginHorizontal(GUILayout.Width(220));
						advReference = (AttitudeReference)GuiUtils.ComboBox.Box((int)advReference, ReferenceTexts, ReferenceTexts);
						GUILayout.EndHorizontal();
						GUILayout.BeginHorizontal(GUILayout.Width(200));
						advDirection = (Vector6.Direction)GuiUtils.ComboBox.Box((int)advDirection, DirectionTexts, DirectionTexts);
						GUILayout.EndHorizontal();
						showForceRol = true;
						break;
				}
				switch(target)
				{
					case MechJebModuleSmartASS.Target.SURFACE_PROGRADE:
					case MechJebModuleSmartASS.Target.SURFACE_RETROGRADE:
						GUILayout.Label("", GUI.skin.label, GUILayout.ExpandWidth(true));
						forceYaw = GUILayout.Toggle(forceYaw, "Heading: ", GUILayout.ExpandWidth(false));
						srfHdg.text = GUILayout.TextField(srfHdg.text, GUILayout.ExpandWidth(false), GUILayout.Width(37));
						GUILayout.Label("°", GUILayout.ExpandWidth(true));
						forcePitch = GUILayout.Toggle(forcePitch, "Pitch: ", GUILayout.ExpandWidth(false));
						srfPit.text = GUILayout.TextField(srfPit.text, GUILayout.ExpandWidth(false), GUILayout.Width(37));
						GUILayout.Label("°", GUILayout.ExpandWidth(true));
						forceRol = GUILayout.Toggle(forceRol, "Roll: ", GUILayout.ExpandWidth(false));
						srfRol.text = GUILayout.TextField(srfRol.text, GUILayout.ExpandWidth(false), GUILayout.Width(37));
						GUILayout.Label("°", GUILayout.ExpandWidth(false));
						break;
					case MechJebModuleSmartASS.Target.SURFACE:
						GUILayout.Label("", GUI.skin.label, GUILayout.ExpandWidth(true));
						forcePitch = GUILayout.Toggle(forcePitch, "Pitch: ", GUILayout.ExpandWidth(false));
						srfVelPit.text = GUILayout.TextField(srfVelPit.text, GUILayout.ExpandWidth(false), GUILayout.Width(37));
						GUILayout.Label("°", GUILayout.ExpandWidth(true));
						forceRol = GUILayout.Toggle(forceRol, "Roll: ", GUILayout.ExpandWidth(false));
						srfVelRol.text = GUILayout.TextField(srfVelRol.text, GUILayout.ExpandWidth(false), GUILayout.Width(37));
						GUILayout.Label("°", GUILayout.ExpandWidth(true));
						forceYaw = GUILayout.Toggle(forceYaw, "Yaw: ", GUILayout.ExpandWidth(false));
						srfVelYaw.text = GUILayout.TextField(srfVelYaw.text, GUILayout.ExpandWidth(false), GUILayout.Width(37));
						GUILayout.Label("°", GUILayout.ExpandWidth(false));
						break;
				}
			}
			GUILayout.Label ("", GUILayout.ExpandWidth (true));
			if (showForceRol)
			{
				forceRol = GUILayout.Toggle(forceRol, "Force Roll: ", GUILayout.ExpandWidth(false));
				rol.text = GUILayout.TextField(rol.text, GUILayout.Width(37));
				GUILayout.Label("°", GUILayout.ExpandWidth(false));
			}
			targetName = TargetTexts[(int)target];
			base.postWindowGUI(windowID);
		}
	}
}
// vim: set noet:
