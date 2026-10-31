using System;
using System.Collections.Generic;
using KodeKit;
using KodeUI;
using KSP.Localization;

namespace MuMech.UI
{
    /// <summary>
    /// The parameter rows of each maneuver Operation (its DoParametersGUI), built once into a panel of ManeuverPlannerView.
    /// An operation without a builder here (e.g. one from another mod) is drawn by its own DoParametersGUI through ImguiBlock.
    /// </summary>
    internal static class OperationPanels
    {
        static readonly Dictionary<Type, Action<OpContext>> builders = new Dictionary<Type, Action<OpContext>>
        {
            { typeof(OperationApoapsis), Apoapsis },
            { typeof(OperationCircularize), c => OpUi.TimeSelectorRows(c.W, c.Panel, OperationCircularize._timeSelector) },
            { typeof(OperationCourseCorrection), CourseCorrection },
            { typeof(OperationEccentricity), Eccentricity },
            { typeof(OperationEllipticize), Ellipticize },
            { typeof(OperationInclination), Inclination },
            { typeof(OperationKillRelVel), c => OpUi.TimeSelectorRows(c.W, c.Panel, OperationKillRelVel._timeSelector) },
            { typeof(OperationLambert), Lambert },
            { typeof(OperationLan), Lan },
            { typeof(OperationLongitude), Longitude },
            { typeof(OperationMoonReturn), MoonReturn },
            { typeof(OperationPeriapsis), Periapsis },
            { typeof(OperationPlane), c => OpUi.TimeSelectorRows(c.W, c.Panel, OperationPlane._timeSelector) },
            { typeof(OperationResonantOrbit), ResonantOrbit },
            { typeof(OperationSemiMajor), SemiMajor },
            { typeof(OperationStationaryOrbit), Stationary },
            { typeof(OperationGeneric), Transfer },
            { typeof(OperationAdvancedTransfer), AdvancedTransferPanel.Build },
        };

        /// <summary>Fill <paramref name="c"/>.Panel with the operation's parameter rows; returns false when it needs the IMGUI fallback.</summary>
        public static bool Build(OpContext c)
        {
            if (!builders.TryGetValue(c.Op.GetType(), out Action<OpContext> build)) return false;
            build(c);
            return true;
        }

        static void Apoapsis(OpContext c)
        {
            var op = (OperationApoapsis)c.Op;
            OpUi.LabeledField(c.W, c.Panel, "#MechJeb_Ap_label1", () => op.NewApA, "km"); //New apoapsis:
            OpUi.TimeSelectorRows(c.W, c.Panel, OperationApoapsis._timeSelector);
        }

        static void Periapsis(OpContext c)
        {
            var op = (OperationPeriapsis)c.Op;
            OpUi.LabeledField(c.W, c.Panel, "#MechJeb_Pe_label", () => op.NewPeA, "km"); //New periapsis:
            OpUi.TimeSelectorRows(c.W, c.Panel, OperationPeriapsis._timeSelector);
        }

        static void SemiMajor(OpContext c)
        {
            var op = (OperationSemiMajor)c.Op;
            OpUi.LabeledField(c.W, c.Panel, "#MechJeb_Sa_label", () => op.NewSma, "km"); //New Semi-Major Axis:
            OpUi.TimeSelectorRows(c.W, c.Panel, OperationSemiMajor._timeSelector);
        }

        static void Eccentricity(OpContext c)
        {
            var op = (OperationEccentricity)c.Op;
            OpUi.LabeledField(c.W, c.Panel, "New eccentricity:", () => op.NewEcc);
            OpUi.TimeSelectorRows(c.W, c.Panel, OperationEccentricity._timeSelector);
        }

        static void Ellipticize(OpContext c)
        {
            var op = (OperationEllipticize)c.Op;
            OpUi.LabeledField(c.W, c.Panel, "#MechJeb_both_label1", () => op.NewPeA, "km"); //New periapsis:
            OpUi.LabeledField(c.W, c.Panel, "#MechJeb_both_label2", () => op.NewApA, "km"); //New apoapsis:
            OpUi.TimeSelectorRows(c.W, c.Panel, OperationEllipticize._timeSelector);
        }

        static void Inclination(OpContext c)
        {
            var op = (OperationInclination)c.Op;
            OpUi.LabeledField(c.W, c.Panel, "#MechJeb_inclination_label", () => op.NewInc, "º"); //New inclination:
            OpUi.TimeSelectorRows(c.W, c.Panel, OperationInclination._timeSelector);
        }

        static void Lambert(OpContext c)
        {
            var op = (OperationLambert)c.Op;
            OpUi.LabeledField(c.W, c.Panel, "#MechJeb_intercept_label", () => op.InterceptInterval, stacked: true); //Time after burn to intercept target:
            OpUi.TimeSelectorRows(c.W, c.Panel, OperationLambert._timeSelector);
        }

        // the angle being edited is the target controller's, shared with the other windows
        static readonly EditableAngle noAngle = new EditableAngle(0);

        static EditableAngle TargetLongitude(OpContext c) => c.Target?.targetLongitude ?? noAngle;

        static void Lan(OpContext c)
        {
            OpUi.TimeSelectorRows(c.W, c.Panel, OperationLan._timeSelector);
            KodeWindow.Label(c.Panel, "#MechJeb_AN_label"); //New Longitude of Ascending Node:
            OpUi.AngleRow(c.W, c.Panel, () => TargetLongitude(c), EditableAngle.Direction.EW);
        }

        static void Longitude(OpContext c)
        {
            OpUi.TimeSelectorRows(c.W, c.Panel, OperationLongitude._timeSelector);
            KodeWindow.Label(c.Panel, "#MechJeb_la_label"); //New Surface Longitude after one orbit:
            OpUi.AngleRow(c.W, c.Panel, () => TargetLongitude(c), EditableAngle.Direction.EW);
        }

        static void MoonReturn(OpContext c)
        {
            var op = (OperationMoonReturn)c.Op;
            OpUi.LabeledField(c.W, c.Panel, "#MechJeb_return_label1", () => op.Periapsis, "km", true); //Approximate final periapsis:
            OpUi.ToggledField(c.W, c.Panel, "Inclination", () => op.InclinationFlag, v => op.InclinationFlag = v, () => op.Inclination, "°");
            KodeWindow.Paragraph(c.Panel, "#MechJeb_return_label2"); //Schedule the burn at the next return window.
        }

        static void CourseCorrection(OpContext c)
        {
            var op = (OperationCourseCorrection)c.Op;

            Layout celestial = KodeWindow.Panel(c.Panel);
            OpUi.LabeledField(c.W, celestial, "#MechJeb_approach_label1", () => op.Periapsis, "km", true); //Approximate final periapsis
            OpUi.ToggledField(c.W, celestial, "Inclination", () => op.InclinationFlag, v => op.InclinationFlag = v, () => op.Inclination, "°");
            OpUi.TimeSelectorRows(c.W, celestial, OperationCourseCorrection._timeSelector);
            c.W.Visible(celestial.gameObject, () => c.TargetIsCelestial);

            Layout other = KodeWindow.Panel(c.Panel);
            OpUi.LabeledField(c.W, other, "#MechJeb_approach_label2", () => op.InterceptDistance, "m", true); //Closest approach distance
            KodeWindow.Paragraph(other, "#MechJeb_approach_label3"); //Schedule the burn to minimize the required ΔV.
            c.W.Visible(other.gameObject, () => !c.TargetIsCelestial);
        }

        static void ResonantOrbit(OpContext c)
        {
            var op = (OperationResonantOrbit)c.Op;

            int num = int.MinValue, den = int.MinValue;
            string text = null;
            string Description()
            {
                if (num != op.ResonanceNumerator.Val || den != op.ResonanceDenominator.Val || text == null)
                {
                    num = op.ResonanceNumerator.Val;
                    den = op.ResonanceDenominator.Val;
                    text = Localizer.Format("#MechJeb_resonant_label1", num + "/" + den); //"Change your orbital period to <<1>> of your current orbital period"
                }

                return text;
            }

            OpUi.DynamicParagraph(c.W, c.Panel, Description);
            Layout row = KodeWindow.Row(c.Panel);
            KodeWindow.Label(row, "#MechJeb_resonant_label2", true); //New orbital period ratio :
            EditUi.Field(c.W, row, () => op.ResonanceNumerator, 30);
            KodeWindow.Label(row, "/");
            EditUi.Field(c.W, row, () => op.ResonanceDenominator, 30);
            OpUi.TimeSelectorRows(c.W, c.Panel, op._timeSelector);
        }

        static void Stationary(OpContext c)
        {
            var op = (OperationStationaryOrbit)c.Op;
            KodeWindow.Label(c.Panel, "#MechJeb_stationary_label1"); //Target longitude:

            // the 10 m steps move the target by the distance on the surface at the target's altitude
            void Move(double meters)
            {
                if (c.Planner.Vessel == null || c.Target == null) return;
                c.Planner.GetOrbitAndUT(out Orbit o, out _);
                op.MoveTargetLongitude(meters, o, c.Target);
            }

            Layout row = KodeWindow.Row(c.Panel);
            OpUi.AngleRow(c.W, row, () => TargetLongitude(c), EditableAngle.Direction.EW, false);
            KodeWindow.IconButton(row, Sprites.Arrow(Sprites.Direction.Left), () => Move(-10));
            KodeWindow.Label(row, "10m");
            KodeWindow.IconButton(row, Sprites.Arrow(Sprites.Direction.Right), () => Move(10));

            // the biome under the target, looked up only when the target moves
            double shownLat = double.NaN, shownLon = double.NaN;
            CelestialBody shownBody = null;
            string biome = "-";
            string Biome()
            {
                MechJebModuleTargetController t = c.Target;
                if (t == null || t.targetBody == null) return biome = "-";
                if (shownBody != t.targetBody || shownLat != t.targetLatitude || shownLon != t.targetLongitude)
                {
                    shownBody = t.targetBody;
                    shownLat = t.targetLatitude;
                    shownLon = t.targetLongitude;
                    biome = t.targetBody.GetExperimentBiomeSafe(shownLat, shownLon);
                    if (string.IsNullOrEmpty(biome)) biome = "-";
                }

                return biome;
            }

            UIText biomeLabel = c.W.BoundLabel(c.Panel, Biome, true);
            c.W.Visible(biomeLabel, () => c.Target != null && c.Target.targetBody != null);

            KodeWindow.Button(c.Panel, "#MechJeb_LandingGuidance_button2", () => c.Target?.PickPositionTargetOnMap()); //Pick target on map
        }

        static void Transfer(OpContext c)
        {
            var op = (OperationGeneric)c.Op;
            KodeWindow w = c.W;

            // two-burn capture vs single-burn intercept/flyby: radio buttons whose texts depend on the kind of target
            void CaptureRadios(Layout row, string captureKey, string interceptKey)
            {
                if (KodeWindow.ModernLayout)
                {
                    w.Segmented(row, new[] { captureKey, interceptKey }, () => op.Capture ? 0 : 1, i => op.Capture = i == 0);
                    return;
                }
                w.Toggle(row, captureKey, () => op.Capture, v =>
                {
                    if (v) op.Capture = true;
                });
                w.Toggle(row, interceptKey, () => !op.Capture, v =>
                {
                    if (v) op.Capture = false;
                });
            }

            Layout celestial = KodeWindow.Row(c.Panel);
            CaptureRadios(celestial, "#MechJeb_Hohm_transfer", "#MechJeb_Hohm_flyby"); //Transfer / Flyby / Impact
            w.Visible(celestial.gameObject, () => c.TargetIsCelestial);
            Layout vessel = KodeWindow.Row(c.Panel);
            CaptureRadios(vessel, "#MechJeb_Hohm_rendezvous", "#MechJeb_Hohm_intercept"); //Rendezvous / Intercept
            w.Visible(vessel.gameObject, () => !c.TargetIsCelestial);

            // match the target's orbit rather than intercepting the target itself
            Layout match = KodeWindow.Panel(c.Panel);
            w.Toggle(match, "#MechJeb_Hohm_matchOrbit", () => op.MatchOrbit, v => op.MatchOrbit = v); //Match orbit
            w.Visible(match.gameObject, () => op.Capture);

            // arrival delay offsets the intercept point
            Layout delay = KodeWindow.Panel(c.Panel);
            OpUi.LabeledField(w, delay, "#MechJeb_Hohm_arrivalDelay", () => op.LagTime, "sec"); //Arrival delay
            w.Visible(delay.gameObject, () => op.Capture && !op.MatchOrbit);

            // optionally create a maneuver node for the arrival burn (OperationGeneric.ViewUpdate clears it while it is not offered)
            Layout plan = KodeWindow.Panel(c.Panel);
            w.Toggle(plan, "#MechJeb_Hohm_createArrivalNode", () => op.PlanCapture, v => op.PlanCapture = v); //Create arrival node
            w.Visible(plan.gameObject, () => op.PlanCaptureAvailable(c.TargetIsCelestial));

            w.Toggle(c.Panel, "#MechJeb_Hohm_simpleTransfer", () => op.Coplanar, v => op.Coplanar = v); //Coplanar only
            OpUi.TimeSelectorRows(w, c.Panel, OperationGeneric._timeSelector);
        }
    }
}
