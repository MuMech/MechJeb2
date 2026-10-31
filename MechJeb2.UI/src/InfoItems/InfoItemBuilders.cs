using System;
using System.Collections.Generic;
using KodeKit;
using KodeUI;
using Smooth.Pools;
using TMPro;
using UnityEngine;

namespace MuMech.UI
{
    /// <summary>
    /// Native versions of the general info items of MechJebModuleInfoItems (docking guidance, phase angles, clipboard copy,
    /// pools), VesselState (RCS and torque readouts), the RCS balancer and controller, SmartASS and SmartRcs.
    /// </summary>
    public static partial class GeneralItemBuilders
    {
        // ---- docking guidance: three axes of a vector in the vessel's frame ----

        /// <summary>
        /// A "(N/A)" label while unavailable, otherwise a header and three live lines "X: +1.23 m/s  [L/J]".
        /// <paramref name="compute"/> fills the three values and runs only while <paramref name="available"/>.
        /// </summary>
        static void ThreeAxes(KodeWindow w, UIObject parent, string naKey, string headerKey, string[] prefixes, string[] suffixes, string template,
            Func<bool> available, Action<double[]> compute)
        {
            UIText na = KodeWindow.Label(parent, naKey);
            w.Visible(na.gameObject, () => !available());

            Layout panel = KodeWindow.Panel(parent);
            w.Visible(panel.gameObject, available);
            KodeWindow.Label(panel, headerKey);

            var chars = new CharText[3];
            for (int i = 0; i < 3; i++)
                chars[i] = new CharText(KodeWindow.LiveLabel(panel, prefixes[i] + template + suffixes[i], false).tmpText);

            var values = new double[3];
            w.AddSync(() =>
            {
                if (!available()) return;
                compute(values);
                for (int i = 0; i < 3; i++)
                {
                    chars[i].Clear().Append(prefixes[i]).PadPositive(values[i], 2).Append(suffixes[i]);
                    chars[i].Commit();
                }
            });
        }

        static void DockingVelocity(KodeWindow w, UIObject p, MechJebModuleInfoItems items)
        {
            MechJebCore core = items.Core;
            ThreeAxes(w, p, "#MechJeb_InfoItems_velocityNA", "#MechJeb_InfoItems_velocity", //"Target-relative velocity: (N/A)" "Target-relative velocity:"
                new[] { "X: ", "Y: ", "Z: " }, new[] { " m/s  [L/J]", " m/s  [I/K]", " m/s  [H/N]" }, "-9999.99",
                () => core.vessel != null && core.Target != null && core.Target.NormalTargetExists,
                v =>
                {
                    Vector3d relVel = core.Target.RelativeVelocity;
                    Transform t = core.vessel.GetTransform();
                    v[0] = Vector3d.Dot(relVel, t.right);
                    v[1] = Vector3d.Dot(relVel, t.forward);
                    v[2] = Vector3d.Dot(relVel, t.up);
                });
        }

        static void DockingAngularVelocity(KodeWindow w, UIObject p, MechJebModuleInfoItems items)
        {
            MechJebCore core = items.Core;
            ThreeAxes(w, p, "#MechJeb_InfoItems_label2", "#MechJeb_InfoItems_label3", //"Target-relative Angular velocity: (N/A)" "Target-relative angular velocity:"
                new[] { "P: ", "Y: ", "R: " }, new[] { " °/s", " °/s", " °/s" }, "-9999.99",
                () => core.vessel != null && core.vessel.ReferenceTransform != null && core.Target != null && core.Target.Target is Vessel,
                v =>
                {
                    var target = (Vessel)core.Target.Target;
                    Vector3d relw = Quaternion.Inverse(core.vessel.ReferenceTransform.rotation) * (target.angularVelocity - core.vessel.angularVelocity) *
                        Mathf.Rad2Deg;
                    v[0] = relw.x;
                    v[1] = relw.z;
                    v[2] = relw.y;
                });
        }

        static void DockingPosition(KodeWindow w, UIObject p, MechJebModuleInfoItems items)
        {
            MechJebCore core = items.Core;
            ThreeAxes(w, p, "#MechJeb_InfoItems_label4", "#MechJeb_InfoItems_label5", //"Separation from target: (N/A)" "Separation from target:"
                new[] { "X: ", "Y: ", "Z: " }, new[] { " m  [L/J]", " m  [I/K]", " m  [H/N]" }, "-99999.99",
                () => core.vessel != null && core.Target != null && core.Target.NormalTargetExists,
                v =>
                {
                    Vector3d sep = core.Target.RelativePosition;
                    Transform t = core.vessel.GetTransform();
                    v[0] = Vector3d.Dot(sep, t.right);
                    v[1] = Vector3d.Dot(sep, t.forward);
                    v[2] = Vector3d.Dot(sep, t.up);
                });
        }

        // ---- phase angles: one row per body, the list depends on where the vessel is ----

        static void PhaseAngles(KodeWindow w, UIObject parent, MechJebModuleInfoItems items, bool planets)
        {
            Layout panel = KodeWindow.Panel(parent);
            UIText title = KodeWindow.Label(panel, planets ? "#MechJeb_InfoItems_label6" : "#MechJeb_InfoItems_label7", true); //"Planet phase angles" "Moon phase angles"
            title.tmpText.alignment = TextAlignmentOptions.Center;

            var rows = new LabelValueRows(panel, "-360.00º");
            MechJebCore core = items.Core;
            w.AddSync(() =>
            {
                if (!rows.CanBuild) return;
                rows.Begin();
                Vessel vessel = core.vessel;
                if (vessel != null && vessel.orbit != null && Planetarium.fetch != null)
                {
                    if (planets) AddPlanetRows(rows, vessel.orbit, core.VesselState.Time);
                    else AddMoonRows(rows, vessel.orbit, core.VesselState.Time);
                }

                rows.End();
            });
        }

        static void AddPlanetRows(LabelValueRows rows, Orbit orbit, double ut)
        {
            CelestialBody sun = Planetarium.fetch.Sun;
            Orbit o = orbit;
            while (o.referenceBody != sun) o = o.referenceBody.orbit;

            List<CelestialBody> bodies = FlightGlobals.Bodies;
            for (int i = 0; i < bodies.Count; i++)
            {
                CelestialBody body = bodies[i];
                if (body == sun || body.referenceBody != sun || body.orbit == o) continue;

                rows.Add(body.bodyName).Append(o.PhaseAngle(body.orbit, ut), 2).Append("º");
            }
        }

        static void AddMoonRows(LabelValueRows rows, Orbit orbit, double ut)
        {
            CelestialBody sun = Planetarium.fetch.Sun;
            if (orbit.referenceBody == sun) return;

            Orbit o = orbit;
            while (o.referenceBody.referenceBody != sun) o = o.referenceBody.orbit;

            List<CelestialBody> moons = o.referenceBody.orbitingBodies;
            for (int i = 0; i < moons.Count; i++)
            {
                CelestialBody body = moons[i];
                if (body.orbit == o) continue;

                rows.Add(body.bodyName).Append(o.PhaseAngle(body.orbit, ut), 2).Append("º");
            }
        }

        // ---- pools status ----

        static void PoolsStatusRows(KodeWindow w, UIObject parent)
        {
            Layout panel = KodeWindow.Panel(parent);
            var rows = new LabelValueRows(panel, "00000/00000");
            var names = new Dictionary<Type, string>();
            w.AddSync(() =>
            {
                if (!rows.CanBuild) return;
                rows.Begin();
                try
                {
                    foreach (KeyValuePair<Type, PoolsStatus> pair in PoolsStatus.poolsInfo)
                    {
                        if (!names.TryGetValue(pair.Key, out string name)) names[pair.Key] = name = MechJebModuleInfoItems.PoolName(pair.Key);
                        rows.Add(name).AppendInt(pair.Value.allocated).Append('/').AppendInt(pair.Value.maxSize);
                    }
                }
                catch (InvalidOperationException)
                {
                    // a pool was registered from another thread while enumerating: show what we have, try again next frame
                }

                rows.End();
            });
        }

        // ---- VesselState ----

        static void VesselDebugString(KodeWindow w, UIObject parent) =>
            InfoText.WrappedLive(w, parent, () => VesselState.Message);

        static void RcsVector(KodeWindow w, UIObject parent, string headerKey, Func<Vector6> source)
        {
            Layout panel = KodeWindow.Panel(parent);
            KodeWindow.Label(panel, headerKey);
            GuidanceUi.Pair(w, panel, "Pos", "[ -99999.999, -99999.999, -99999.999 ]", ct => ct.PrettyPrint(source().Positive));
            GuidanceUi.Pair(w, panel, "Neg", "[ -99999.999, -99999.999, -99999.999 ]", ct => ct.PrettyPrint(source().Negative));
        }

        // "Torque sources": names on the left, the largest component-wise torque of each source on the right
        static void TorqueCompare(KodeWindow w, UIObject parent, VesselState vs)
        {
            Layout panel = KodeWindow.Panel(parent);
            KodeWindow.Label(panel, "Torque sources");
            Layout row = KodeWindow.Row(panel);
            Layout names = InfoText.Column(row, true);
            Layout values = InfoText.Column(row, false);
            string[] labels = { "ReactionWheel", "RCS", "ControlSurface", "Gimbal", "Diff Throttle", "Others (FAR)" };
            var chars = new CharText[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                KodeWindow.Label(names, labels[i]);
                chars[i] = new CharText(KodeWindow.LiveLabel(values, "[ -99999.999, -99999.999, -99999.999 ]", false).tmpText);
            }

            w.AddSync(() =>
            {
                Vector6 diff = vs.DifferentialThrottleTorque;
                Vector3d diffTorque = Vector3d.Max(diff.Positive, diff.Negative);
                diffTorque.y = 0;
                chars[0].Clear().PrettyPrint(Vector3d.Max(vs.TorqueReactionWheel.Positive, vs.TorqueReactionWheel.Negative));
                chars[1].Clear().PrettyPrint(Vector3d.Max(vs.RCSTorqueAvailable.Positive, vs.RCSTorqueAvailable.Negative));
                chars[2].Clear().PrettyPrint(Vector3d.Max(vs.TorqueControlSurface.Positive, vs.TorqueControlSurface.Negative));
                chars[3].Clear().PrettyPrint(Vector3d.Max(vs.TorqueGimbal.Positive, vs.TorqueGimbal.Negative));
                chars[4].Clear().PrettyPrint(diffTorque);
                chars[5].Clear().PrettyPrint(Vector3d.Max(vs.TorqueOthers.Positive, vs.TorqueOthers.Negative));
                for (int i = 0; i < chars.Length; i++) chars[i].Commit();
            });
        }

        // ---- RCS balancer ----

        static void RcsBalancerInfo(KodeWindow w, UIObject parent, MechJebModuleRCSBalancer b)
        {
            Layout panel = KodeWindow.Panel(parent);
            RCSSolverThread solver = b.SolverThread;
            GuidanceUi.Pair(w, panel, "#MechJeb_RCSBalancerInfo_Label1", "99999 ms", ct => ct.Append(solver.CalculationTime * 1000, 0).Append(" ms")); //"Calculation time"
            GuidanceUi.Pair(w, panel, "#MechJeb_RCSBalancerInfo_Label2", "99999", ct => ct.AppendInt(solver.TaskCount)); //"Pending tasks"
            GuidanceUi.Pair(w, panel, "#MechJeb_RCSBalancerInfo_Label3", "99999", ct => ct.AppendInt(solver.CacheSize)); //"Cache size"
            GuidanceUi.Pair(w, panel, "#MechJeb_RCSBalancerInfo_Label4", "99999", ct => ct.AppendInt(solver.CacheHits)); //"Cache hits"
            GuidanceUi.Pair(w, panel, "#MechJeb_RCSBalancerInfo_Label5", "99999", ct => ct.AppendInt(solver.CacheMisses)); //"Cache misses"

            GuidanceUi.Pair(w, panel, "#MechJeb_RCSBalancerInfo_Label6", "-1.234 km", ct => ct.AppendSI(b.ComError).Append('m')); //"CoM shift"
            GuidanceUi.Pair(w, panel, "#MechJeb_RCSBalancerInfo_Label7", "-1.234 km", ct => ct.AppendSI(b.ComErrorThreshold).Append('m')); //"CoM recalc"
            GuidanceUi.Pair(w, panel, "#MechJeb_RCSBalancerInfo_Label8", "-1.234 km", ct => ct.AppendSI(b.MaxComError).Append('m')); //"Max CoM shift"

            GuidanceUi.Pair(w, panel, "#MechJeb_RCSBalancerInfo_Label9", "working idle", ct => ct.Append(solver.StatusString)); //"Status"

            UIText error = InfoText.WrappedLive(w, panel, () => solver.ErrorString);
            w.Visible(error.gameObject, () => !string.IsNullOrEmpty(solver.ErrorString));
        }

        // "(power:f0,f1,...) (power:...)": the thrusters of every RCS part, scaled to 0-9
        static void RcsThrusterStates(KodeWindow w, UIObject parent, MechJebModuleRCSBalancer b)
        {
            Layout panel = KodeWindow.Panel(parent);
            KodeWindow.Label(panel, "#MechJeb_RCSThrusterStates_Label1"); //"RCS thrusters states (scaled to 0-9)"
            CharText states = InfoText.WrappedChars(panel, out _);
            MechJebCore core = b.Core;
            w.AddSync(() =>
            {
                states.Clear();
                Vessel vessel = core.vessel;
                bool first = true;
                if (vessel != null)
                {
                    List<Part> parts = vessel.parts;
                    for (int i = 0; i < parts.Count; i++)
                    {
                        PartModuleList modules = parts[i].Modules;
                        for (int j = 0; j < modules.Count; j++)
                        {
                            if (!(modules[j] is ModuleRCS pm)) continue;
                            if (!first) states.Append(' ');
                            first = false;
                            states.Append('(').Append(pm.thrusterPower * 9, 0).Append(':');
                            for (int k = 0; k < pm.thrustForces.Length; k++)
                            {
                                if (k != 0) states.Append(',');
                                states.Append(pm.thrustForces[k] * 9, 0);
                            }

                            states.Append(')');
                        }
                    }
                }

                if (states.Length == 0) states.Append(' ');
                states.Commit();
            });
        }

        static void RcsPartThrottles(KodeWindow w, UIObject parent, MechJebModuleRCSBalancer b)
        {
            Layout panel = KodeWindow.Panel(parent);
            CharText throttles = InfoText.WrappedChars(panel, out _);
            MechJebCore core = b.Core;
            w.AddSync(() =>
            {
                throttles.Clear();
                Vessel vessel = core.vessel;
                bool first = true;
                if (vessel != null)
                {
                    List<Part> parts = vessel.parts;
                    for (int i = 0; i < parts.Count; i++)
                    {
                        PartModuleList modules = parts[i].Modules;
                        for (int j = 0; j < modules.Count; j++)
                        {
                            if (!(modules[j] is ModuleRCS pm)) continue;
                            if (!first) throttles.Append(' ');
                            first = false;
                            throttles.Append(pm.thrusterPower, 1);
                        }
                    }
                }

                if (throttles.Length == 0) throttles.Append(' ');
                throttles.Commit();
            });
        }

        static void ControlVector(KodeWindow w, UIObject parent)
        {
            Layout panel = KodeWindow.Panel(parent);
            Func<bool> have = () => FlightInputHandler.state != null;
            GuidanceUi.Pair(w, panel, "X/Y/Z", "-1.00 -1.00 -1.00", ct =>
            {
                FlightCtrlState s = FlightInputHandler.state;
                ct.Append(s.X, 2).Append(' ').Append(s.Y, 2).Append(' ').Append(s.Z, 2);
            }, have);
            GuidanceUi.Pair(w, panel, "R/P/Y", "-1.00 -1.00 -1.00", ct =>
            {
                FlightCtrlState s = FlightInputHandler.state;
                ct.Append(s.roll, 2).Append(' ').Append(s.pitch, 2).Append(' ').Append(s.yaw, 2);
            }, have);
        }

        // ---- RCS controller PID ----

        static void RcsPid(KodeWindow w, UIObject parent, MechJebModuleRCSController c)
        {
            Layout panel = KodeWindow.Panel(parent);
            w.Toggle(panel, "RCS Manual Pid", () => c.rcsManualPID, v => c.rcsManualPID = v);

            // manual: P, I and D are fields
            Layout manual = KodeWindow.Row(panel);
            w.Visible(manual.gameObject, () => c.rcsManualPID);
            KodeWindow.Label(manual, "P");
            EditUi.Field(w, manual, () => c.Kp, 60);
            KodeWindow.Label(manual, "I");
            EditUi.Field(w, manual, () => c.Ki, 60);
            KodeWindow.Label(manual, "D");
            EditUi.Field(w, manual, () => c.Kd, 60);

            // automatic: Tf is a field, P, I and D are computed
            Layout auto = KodeWindow.Panel(panel);
            w.Visible(auto.gameObject, () => !c.rcsManualPID);
            Layout tf = KodeWindow.Row(auto);
            KodeWindow.Label(tf, "Tf");
            EditUi.Field(w, tf, () => c.Tf, 40);
            Layout pid = KodeWindow.Row(auto);
            KodeWindow.Label(pid, "P");
            GuidanceUi.Live(w, pid, "0.0000", true, ct => ct.Append(c.Kp.Val, 4));
            KodeWindow.Label(pid, "I");
            GuidanceUi.Live(w, pid, "0.0000", true, ct => ct.Append(c.Ki.Val, 4));
            KodeWindow.Label(pid, "D");
            GuidanceUi.Live(w, pid, "0.0000", true, ct => ct.Append(c.Kd.Val, 4));

            // the IMGUI item pushed the parameters into the PID controller on every draw. Manual mode only copies three
            // numbers. Automatic mode derives them from Tf and allocates (Tf = Math.Max(...), the fields' new texts), so it
            // runs when the item comes into view, the mode changes or Tf changes (Equals: a NaN Tf is not a change)
            bool pushed = false, pushedManual = false;
            double pushedTf = 0;
            w.AddSync(() =>
            {
                if (!panel.gameObject.activeInHierarchy)
                {
                    pushed = false;
                    return;
                }

                bool manual = c.rcsManualPID;
                if (manual || !pushed || pushedManual || !c.Tf.Val.Equals(pushedTf)) c.setPIDParameters();
                pushed = true;
                pushedManual = manual;
                pushedTf = c.Tf.Val;
            });
        }
    }
}
