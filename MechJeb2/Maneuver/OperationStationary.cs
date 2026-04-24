extern alias JetBrainsAnnotations;
using System;
using System.Collections.Generic;
using JetBrainsAnnotations::JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationStationaryOrbit : Operation
    {
        [Persistent] public double targetLongitude = 0;
        [Persistent] public double targetLatitude = 0;

        public override string GetName() => "stationary orbit"; //stationary orbit
        
        private void MoveByMeter(ref EditableAngle angle, double distance, double alt, Orbit o)
        {
            double angularDelta = distance * UtilMath.Rad2Deg / (alt + o.referenceBody.Radius);
            angle.Val += angularDelta;
        }

        public override void DoParametersGUI(Orbit o, double UT, ITargetable target)
        {
            double asl = o.referenceBody.TerrainAltitude(Core.Target.targetLatitude, Core.Target.targetLongitude);
            
            GUILayout.BeginVertical();

            if (!Core.Target.PositionTargetExists)
            {
                Core.Target.SetPositionTarget(o.referenceBody, targetLatitude, targetLongitude);
            }

            GUILayout.Label(Localizer.Format("#MechJeb_stationary_label1")); //Target longitude:
            
            GUILayout.BeginHorizontal();
            Core.Target.targetLongitude.DrawEditGUI(EditableAngle.Direction.EW);
            
            if (GUILayout.Button("◄"))
            {
                MoveByMeter(ref Core.Target.targetLongitude, -10, asl, o);
            }

            GUILayout.Label("10m");
            if (GUILayout.Button("►"))
            {
                MoveByMeter(ref Core.Target.targetLongitude, 10, asl, o);
            }
            GUILayout.EndHorizontal();

            if (Core.Target.targetBody != null)
            {
                GUILayout.Label(Core.Target.targetBody.GetExperimentBiomeSafe(Core.Target.targetLatitude, Core.Target.targetLongitude));
            }

            if (GUILayout.Button(Localizer.Format("#MechJeb_LandingGuidance_button2"))) Core.Target.PickPositionTargetOnMap(); //Pick target on map

            targetLatitude = 0;
            targetLongitude = Core.Target.targetLongitude;

            GUILayout.EndVertical();
        }

        protected override List<ManeuverParameters> MakeNodesImpl(Orbit o, double ut, MechJebModuleTargetController target)
        {
            double syncSMA = Math.Pow(o.referenceBody.gravParameter * Math.Pow(o.referenceBody.rotationPeriod / (2.0 * Math.PI), 2.0), 1.0 / 3.0);
            
            if (syncSMA > o.referenceBody.hillSphere)
            {
                throw new OperationException(Localizer.Format("#MechJeb_stationary_Exception1", o.referenceBody.displayName.LocalizeRemoveGender()));
            }

            double currentBodyRotationRad = (o.referenceBody.rotationAngle + (360.0 * (ut / o.referenceBody.rotationPeriod))) * Math.PI / 180.0;
            double targMNA = MuMech.OrbitExtensions.ClampRadians((targetLongitude * Math.PI / 180.0) + currentBodyRotationRad);

            Orbit targOrbit = new Orbit();
            targOrbit.referenceBody = o.referenceBody;
            targOrbit.semiMajorAxis = syncSMA;
            targOrbit.eccentricity = 0;
            targOrbit.inclination = 0;
            targOrbit.LAN = 0;
            targOrbit.argPeriapsis = 0;
            targOrbit.meanAnomalyAtEpoch = targMNA;
            targOrbit.epoch = ut;
            targOrbit.Init();

            (Vector3d dV1, double ut1, Vector3d dV2, double ut2) =
                OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(o, targOrbit, ut, 0, false, false, true, true);

            return new List<ManeuverParameters>
            {
                new ManeuverParameters(dV1, ut1),
                new ManeuverParameters(dV2, ut2)
            };
        }
    }
}