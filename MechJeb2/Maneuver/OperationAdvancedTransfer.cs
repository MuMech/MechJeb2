using System;
using UnityEngine;

namespace MuMech
{
	public class OperationAdvancedTransfer : Operation
	{
		enum Mode
		{
			LimitedTime,
			Porkchop
		}
		static string[] modeNames = {"Limited time", "Porkchop selection"};
		public override string getName() { return "advanced transfer to another planet";}

		double minDepartureTime;
		double minTransferTime;
		double maxDepartureTime;
		double maxTransferTime;

		public EditableTime maxArrivalTime = new EditableTime();

		const double minSamplingStep = 12 * 3600;

		private Mode selectionMode = Mode.Porkchop;
		int windowWidth;

		TransferCalculator worker;
		private PlotArea plot;

		bool _draggable = true;
		public override bool draggable { get { return _draggable;}}

		const int porkchop_Height = 200;

		private string CheckPreconditions(Orbit o, MechJebModuleTargetController target)
		{
			if (o.eccentricity >= 1 || o.ApR >= o.referenceBody.sphereOfInfluence)
				return "initial orbit must not be hyperbolic";

			if (!target.NormalTargetExists)
				return "must select a target for the interplanetary transfer.";

			if (o.referenceBody.referenceBody == null)
				return "doesn't make sense to plot an interplanetary transfer from an orbit around " + o.referenceBody.theName + ".";

			if (o.referenceBody.referenceBody != target.TargetOrbit.referenceBody)
			{
				if (o.referenceBody == target.TargetOrbit.referenceBody)
					return "use regular Hohmann transfer function to intercept another body orbiting " + o.referenceBody.theName + ".";
				return "an interplanetary transfer from within " + o.referenceBody.theName + "'s sphere of influence must target a body that orbits " + o.referenceBody.theName + "'s parent, " + o.referenceBody.referenceBody.theName + ".";
			}
			return null;
		}

		void ComputeStuff(Orbit o, double universalTime, MechJebModuleTargetController target)
		{
			errorMessage = CheckPreconditions(o, target);
			if (errorMessage == null)
				errorMessage = "";
			else
				return;

			if (worker != null)
				worker.stop = true;
			plot = null;

			switch (selectionMode)
			{
			case Mode.LimitedTime:
				worker = new TransferCalculator (o, target.TargetOrbit, universalTime, maxArrivalTime, minSamplingStep);
				break;
			case Mode.Porkchop:
				worker = new AllGraphTransferCalculator(o, target.TargetOrbit, minDepartureTime, maxDepartureTime, minTransferTime, maxTransferTime, windowWidth, porkchop_Height);
				break;
			}
		}

		void ComputeTimes(Orbit o, Orbit destination, double universalTime)
		{
			if (destination == null || o == null || o.referenceBody.orbit == null)
				return;

			double synodic_period = o.referenceBody.orbit.SynodicPeriod(destination);
			double hohmann_transfer_time = OrbitUtil.GetTransferTime(o.referenceBody.orbit, destination);

			minDepartureTime = universalTime;
			minTransferTime = 3600;

			maxDepartureTime = minDepartureTime + synodic_period * 1.5;
			maxTransferTime = hohmann_transfer_time * 1.5;
			maxArrivalTime.val = (synodic_period + hohmann_transfer_time) * 1.5;
		}

		private void DoPorkchopGui(Orbit o, double universalTime, MechJebModuleTargetController target)
		{
			if (worker == null)
				return;
			string dv = " - ";
			string departure = " - ";
			string duration = " - ";
			if (worker.Finished && worker.computed.GetLength(1) == porkchop_Height)
			{
				if (plot == null && Event.current.type == EventType.Layout)
				{
					plot = new PlotArea(
						worker.minDepartureTime,
						worker.maxDepartureTime,
						worker.minTransferTime,
						worker.maxTransferTime,
						new Porkchop(worker.computed).texture,
						(xmin, xmax, ymin, ymax) => {
							minDepartureTime = Math.Max(xmin, universalTime);
							maxDepartureTime = xmax;
							minTransferTime = Math.Max(ymin, 3600);
							maxTransferTime = ymax;
							GUI.changed = true;
						});
					plot.selectedPoint = new int[]{worker.bestDate, worker.bestDuration};
				}
			}
			if (plot != null)
			{
				var point = plot.selectedPoint;
				if (plot.hoveredPoint != null)
					point = plot.hoveredPoint;

				var p = worker.computed[point[0], point[1]];
				if (p != null)
				{
					dv = MuUtils.ToSI(p.dV.magnitude) + "m/s";
					if (worker.DateFromIndex(point[0]) < Planetarium.GetUniversalTime())
						departure = "any time now";
					else
						departure = GuiUtils.TimeToDHMS(worker.DateFromIndex(point[0]) - Planetarium.GetUniversalTime());
					duration = GuiUtils.TimeToDHMS(worker.DurationFromIndex(point[1]));
				}
				plot.DoGUI();
				if (!plot.draggable) _draggable = false;
			}
			else
			{
				GUIStyle progressStyle = new GUIStyle
				{
					font = GuiUtils.skin.font,
					fontSize = GuiUtils.skin.label.fontSize,
					fontStyle = GuiUtils.skin.label.fontStyle,
					normal = {textColor = GuiUtils.skin.label.normal.textColor}
				};

				GUILayout.Box("Computing: " + worker.Progress + "%", progressStyle, new GUILayoutOption[] {
					GUILayout.Width(windowWidth),
					GUILayout.Height(porkchop_Height)});
			}
			GUILayout.BeginHorizontal();
			GUILayout.Label("ΔV: " + dv);
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Reset", GuiUtils.yellowOnHover))
				ComputeTimes(o, target.TargetOrbit, universalTime);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Select: ");
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Lowest ΔV"))
			{
				plot.selectedPoint = new int[]{ worker.bestDate, worker.bestDuration };
				GUI.changed = false;
			}

			if (GUILayout.Button("ASAP"))
			{
				int bestDuration = 0;
				for (int i = 1; i < worker.computed.GetLength(1); i++)
				{
					if (worker.computed[0, bestDuration].dV.sqrMagnitude > worker.computed[0, i].dV.sqrMagnitude)
						bestDuration = i;
				}
				plot.selectedPoint = new int[]{ 0, bestDuration };
				GUI.changed = false;
			}
			GUILayout.EndHorizontal();

			GUILayout.Label("Departure in " + departure);
			GUILayout.Label("Transit duration " + duration);
		}

		public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
		{
			_draggable = true;
			if (worker != null && !target.NormalTargetExists && Event.current.type == EventType.Layout)
			{
				worker.stop = true;
				worker = null;
				plot = null;
			}

			selectionMode = (Mode) GuiUtils.ComboBox.Box((int) selectionMode, modeNames, this);
			if (Event.current.type == EventType.Repaint)
				windowWidth = (int)GUILayoutUtility.GetLastRect().width;

			switch (selectionMode)
			{
			case Mode.LimitedTime:
				GuiUtils.SimpleTextBox("Max arrival time", maxArrivalTime);
				if (worker != null && !worker.Finished)
					GuiUtils.SimpleLabel("Computing: " + worker.Progress + "%");
				break;
			case Mode.Porkchop:
				DoPorkchopGui(o, universalTime, target);
				break;
			}

			if (worker == null || worker.destinationOrbit != target.TargetOrbit || worker.originOrbit != o)
				ComputeTimes(o, target.TargetOrbit, universalTime);

			if (GUI.changed || worker == null || worker.destinationOrbit != target.TargetOrbit || worker.originOrbit != o)
				ComputeStuff(o, universalTime, target);
		}

		public override ManeuverParameters MakeNodeImpl(Orbit o, double UT, MechJebModuleTargetController target)
		{
			// Check preconditions
			string message = CheckPreconditions(o, target);
			if (message != null)
				throw new OperationException(message);

			// Check if computation is finished
			if (worker != null && !worker.Finished)
				throw new OperationException("Computation not finished");
			if (worker == null)
			{
				ComputeStuff(o, UT, target);
				throw new OperationException("Started computation");
			}

			if (worker.result == null)
			{
				throw new OperationException("Computation failed");
			}
			if (selectionMode == Mode.Porkchop)
			{
				if (plot == null || plot.selectedPoint == null)
					throw new OperationException("Invalid point selected.");
				return TransferCalculator.OptimizeEjection(
					worker.computed[plot.selectedPoint[0], plot.selectedPoint[1]],
					o, worker.destinationOrbit,
					worker.DateFromIndex(plot.selectedPoint[0]) + worker.DurationFromIndex(plot.selectedPoint[1]),
					UT);
			}

			return worker.OptimizedResult;
		}
	}
}

